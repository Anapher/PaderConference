using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Strive.Core.Interfaces.Gateways.Repositories;
using Strive.Core.Services.ConferenceControl.ClientControl;
using Strive.Core.Services.ConferenceControl.Gateways;
using Strive.Core.Services.ConferenceControl.Notifications;
using Strive.Core.Services.ConferenceControl.Requests;

namespace Strive.Core.Services.ConferenceControl.UseCases
{
    public class JoinConferenceRequestHandler : IRequestHandler<JoinConferenceRequest>
    {
        private readonly IMediator _mediator;
        private readonly IJoinedParticipantsRepository _joinedParticipantsRepository;
        private readonly ILogger<JoinConferenceRequestHandler> _logger;

        public JoinConferenceRequestHandler(IMediator mediator,
            IJoinedParticipantsRepository joinedParticipantsRepository, ILogger<JoinConferenceRequestHandler> logger)
        {
            _mediator = mediator;
            _joinedParticipantsRepository = joinedParticipantsRepository;
            _logger = logger;
        }

        public async Task<Unit> Handle(JoinConferenceRequest request, CancellationToken cancellationToken)
        {
            var (participant, connectionId, meta) = request;
            var (conferenceId, participantId) = participant;

            _logger.LogDebug("Participant {participantId} is joining conference {conferenceId}", participantId,
                conferenceId);

            var previousSession = await _joinedParticipantsRepository.AddParticipant(participant, connectionId);
            if (previousSession != null)
            {
                _logger.LogDebug("The participant {participantId} was already joined, kick existing connection.",
                    participantId);
                await _mediator.Publish(new ParticipantKickedNotification(
                    new Participant(previousSession.ConferenceId, participantId), previousSession.ConnectionId,
                    ParticipantKickedReason.NewSessionConnected));
            }

            await using var @lock = await _joinedParticipantsRepository.LockParticipantJoin(participant);

            if (!await _joinedParticipantsRepository.IsParticipantJoined(participant, connectionId))
                throw new ConcurrencyException("Race condition on participant join.");

            _logger.LogDebug("Begin joining of participant {participant}", participant);

            // enable messaging just after kicking client
            await _mediator.Publish(new ParticipantInitializedNotification(participant));

            // do not merge these together as handlers for ParticipantJoinedNotification may want to send messages to the participant
            await _mediator.Send(new EnableParticipantMessagingRequest(participant, connectionId), cancellationToken);

            await _mediator.Publish(new ParticipantJoinedNotification(participant, meta));

            return Unit.Value;
        }
    }
}
