﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using PaderConference.Core.Services.ConferenceControl.Notifications;
using PaderConference.Core.Services.Rooms.Gateways;
using PaderConference.Core.Services.Rooms.Notifications;

namespace PaderConference.Core.Services.Rooms.NotificationHandlers
{
    public class ConferenceClosedNotificationHandler : INotificationHandler<ConferenceClosedNotification>
    {
        private readonly IRoomRepository _roomRepository;
        private readonly IMediator _mediator;

        public ConferenceClosedNotificationHandler(IRoomRepository roomRepository, IMediator mediator)
        {
            _roomRepository = roomRepository;
            _mediator = mediator;
        }

        public async Task Handle(ConferenceClosedNotification notification, CancellationToken cancellationToken)
        {
            var conferenceId = notification.ConferenceId;

            // we can not use the other UseCases here as they implement custom logic to keep the participants in the room system,
            // here we especially want to kick all participants from all rooms

            // concurrency: as we delete all participants and all rooms at once, all future SetParticipantRoomUseCases will fail,
            // as no rooms exist any more, so it is impossible that new mappings will be created

            var result = await _roomRepository.DeleteAllRoomsAndMappingsOfConference(conferenceId);
            await _mediator.Publish(new RoomsRemovedNotification(conferenceId, result.DeletedRooms));
            await _mediator.Publish(new ParticipantsRoomChangedNotification(conferenceId,
                result.DeletedParticipants.Select(participantId => new Participant(conferenceId, participantId))));
        }
    }
}