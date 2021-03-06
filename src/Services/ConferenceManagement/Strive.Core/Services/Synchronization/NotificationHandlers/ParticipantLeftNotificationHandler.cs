using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Strive.Core.Services.ConferenceControl.Notifications;
using Strive.Core.Services.Synchronization.Gateways;
using Strive.Core.Services.Synchronization.Notifications;

namespace Strive.Core.Services.Synchronization.NotificationHandlers
{
    public class ParticipantLeftNotificationHandler : INotificationHandler<ParticipantLeftNotification>
    {
        private readonly IMediator _mediator;
        private readonly ISynchronizedObjectSubscriptionsRepository _subscriptionsRepository;

        public ParticipantLeftNotificationHandler(IMediator mediator,
            ISynchronizedObjectSubscriptionsRepository subscriptionsRepository)
        {
            _mediator = mediator;
            _subscriptionsRepository = subscriptionsRepository;
        }

        public async Task Handle(ParticipantLeftNotification notification, CancellationToken cancellationToken)
        {
            var (participant, _) = notification;

            var removedSubscriptions = await _subscriptionsRepository.Remove(participant);
            if (removedSubscriptions?.Any() == true)
                await _mediator.Publish(
                    new ParticipantSubscriptionsUpdatedNotification(participant,
                        removedSubscriptions.Select(SynchronizedObjectId.Parse).ToList(),
                        ImmutableList<SynchronizedObjectId>.Empty), cancellationToken);
        }
    }
}
