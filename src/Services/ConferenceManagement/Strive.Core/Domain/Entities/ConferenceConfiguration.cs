using System;
using System.Collections.Generic;
using Strive.Core.Services.Chat;
using Strive.Core.Services.Scenes;

namespace Strive.Core.Domain.Entities
{
    public class ConferenceConfiguration : IScheduleInfo
    {
        /// <summary>
        ///     The name of the conference. May be null
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        ///     Participant ids of moderators
        /// </summary>
        public IList<string> Moderators { get; set; } = new List<string>();

        /// <summary>
        ///     The starting time of this conference. If <see cref="ScheduleCron" /> is not null, this is the first time the
        ///     conference starts
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        ///     A cron string that determines the scheduled time of this conference. The schedule starts after
        ///     <see cref="StartTime" />
        /// </summary>
        public string? ScheduleCron { get; set; }

        /// <summary>
        ///     Chat options
        /// </summary>
        public ChatOptions Chat { get; init; } = new();

        /// <summary>
        ///     Scene options
        /// </summary>
        public SceneOptions Scenes { get; init; } = new();
    }
}
