﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JsonPatchGenerator;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.SignalR;
using PaderConference.Core.Services.Synchronization.Notifications;

namespace PaderConference.Hubs.NotificationHandlers
{
    public class
        SynchronizedObjectUpdatedNotificationHandler : INotificationHandler<SynchronizedObjectUpdatedNotification>
    {
        private readonly IHubContext<CoreHub> _hubContext;

        public SynchronizedObjectUpdatedNotificationHandler(IHubContext<CoreHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task Handle(SynchronizedObjectUpdatedNotification notification,
            CancellationToken cancellationToken)
        {
            var participantGroups = notification.Participants.Select(CoreHubGroups.OfParticipant);

            if (notification.PreviousValue == null)
            {
                var payload = new SyncObjPayload<object>(notification.SyncObjId, notification.Value);
                await _hubContext.Clients.Groups(participantGroups)
                    .OnSynchronizeObjectState(payload, cancellationToken);
            }
            else
            {
                var patch = JsonPatchFactory.CreatePatch(notification.PreviousValue, notification.Value);
                var payload = new SyncObjPayload<JsonPatchDocument>(notification.SyncObjId, patch);

                await _hubContext.Clients.Groups(participantGroups)
                    .OnSynchronizedObjectUpdated(payload, cancellationToken);
            }
        }
    }
}