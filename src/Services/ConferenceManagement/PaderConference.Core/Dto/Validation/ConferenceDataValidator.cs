﻿using System;
using FluentValidation;
using PaderConference.Core.Dto.Services;
using PaderConference.Core.Extensions;
using PaderConference.Core.Services.Permissions;

namespace PaderConference.Core.Dto.Validation
{
    public class ConferenceDataValidator : AbstractValidator<ConferenceData>
    {
        public ConferenceDataValidator()
        {
            RuleFor(x => x.Configuration).NotNull();
            RuleFor(x => x.Configuration.Moderators).NotEmpty();
            RuleFor(x => x.Configuration.ScheduleCron).Must(x =>
            {
                if (x == null) return true;
                try
                {
                    CronYearParser.GetNextOccurrence(x, DateTimeOffset.UtcNow, TimeZoneInfo.Utc);
                }
                catch (Exception)
                {
                    return false;
                }

                return true;
            });

            RuleFor(x => x.Configuration.Chat.CancelParticipantIsTypingInterval).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Configuration.Chat.CancelParticipantIsTypingAfter).GreaterThanOrEqualTo(1);
            RuleFor(x => x.Configuration.Chat.MaxChatMessageHistory).GreaterThanOrEqualTo(1);

            RuleForEach(x => x.Permissions).ChildRules(group =>
            {
                group.RuleFor(x => x.Key).IsInEnum();
                group.RuleForEach(x => x.Value).Must(x => PermissionsListUtil.All.ContainsKey(x.Key))
                    .WithMessage(x => $"The permission key {x.Key} was not found.")
                    .Must(x => PermissionsListUtil.All.TryGetValue(x.Key, out var descriptor) &&
                               descriptor.ValidateValue(x.Value)).WithMessage(x =>
                        $"The value of permission key {x.Key} doesn't match value type.");
            });
        }
    }
}