using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Strive.Core.Services.Chat.Channels;
using Strive.Core.Services.Chat.Requests;

namespace Strive.Core.Services.Chat
{
    public class ParticipantTypingTimer : IParticipantTypingTimer
    {
        private readonly IMediator _mediator;
        private readonly ITaskDelay _taskDelay;
        private readonly object _lock = new();
        private readonly Dictionary<ParticipantInChannel, DateTimeOffset> _timers = new();
        private CancellationTokenSource? _cancellationTokenSource;

        public ParticipantTypingTimer(IMediator mediator, ITaskDelay taskDelay)
        {
            _mediator = mediator;
            _taskDelay = taskDelay;
        }

        public void RemoveParticipantTypingAfter(Participant participant, ChatChannel channel, TimeSpan timespan)
        {
            var now = DateTimeOffset.UtcNow;
            var timeout = now.Add(timespan);

            lock (_lock)
            {
                var info = new ParticipantInChannel(participant, channel);
                _timers[info] = timeout;

                Reschedule();
            }
        }

        public IEnumerable<ChatChannel> CancelAllTimersOfParticipant(Participant participant)
        {
            lock (_lock)
            {
                var participantChannels = _timers.Keys.Where(x => x.Participant.Equals(participant)).ToList();
                foreach (var participantChannel in participantChannels)
                {
                    _timers.Remove(participantChannel);
                }

                Reschedule();
                return participantChannels.Select(x => x.Channel).ToList();
            }
        }

        public void CancelTimer(Participant participant, ChatChannel channel)
        {
            lock (_lock)
            {
                var info = new ParticipantInChannel(participant, channel);
                if (_timers.Remove(info)) Reschedule();
            }
        }

        public void CancelAllTimersOfConference(string conferenceId)
        {
            lock (_lock)
            {
                var timersToRemove = _timers.Keys.Where(x => x.Participant.ConferenceId == conferenceId).ToList();
                foreach (var participantInChannel in timersToRemove)
                {
                    _timers.Remove(participantInChannel);
                }

                if (timersToRemove.Any()) Reschedule();
            }
        }

        private async void Reschedule()
        {
            while (true)
            {
                ParticipantInChannel nextParticipant;
                DateTimeOffset nextTime;
                CancellationToken token;

                lock (_lock)
                {
                    // cancel existing cancellation token and set new token
                    if (_cancellationTokenSource != null)
                    {
                        _cancellationTokenSource.Cancel();
                        _cancellationTokenSource.Dispose();
                        _cancellationTokenSource = null;
                    }

                    if (!_timers.Any()) return;

                    var cancellationTokenSource = _cancellationTokenSource = new CancellationTokenSource();
                    token = cancellationTokenSource.Token;

                    (nextParticipant, nextTime) = _timers.OrderBy(x => x.Value).First();
                }

                var timeLeft = nextTime.Subtract(DateTimeOffset.UtcNow);
                if (timeLeft > TimeSpan.Zero)
                    try
                    {
                        await _taskDelay.Delay(timeLeft, token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                bool remove;
                lock (_lock)
                {
                    remove = _timers.Remove(nextParticipant);
                }

                if (remove) _ = RemoveParticipantTyping(nextParticipant);
            }
        }

        private async Task RemoveParticipantTyping(ParticipantInChannel participant)
        {
            await _mediator.Send(new SetParticipantTypingRequest(participant.Participant, participant.Channel, false));
        }

        private record ParticipantInChannel(Participant Participant, ChatChannel Channel);
    }
}
