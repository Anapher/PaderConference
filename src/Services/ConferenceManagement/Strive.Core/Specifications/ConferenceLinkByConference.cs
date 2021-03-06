using System;
using System.Linq.Expressions;
using Strive.Core.Domain.Entities;
using SpeciVacation;

namespace Strive.Core.Specifications
{
    public class ConferenceLinkByConference : Specification<ConferenceLink>
    {
        private readonly string _conferenceId;

        public ConferenceLinkByConference(string conferenceId)
        {
            _conferenceId = conferenceId;
        }

        public override Expression<Func<ConferenceLink, bool>> ToExpression()
        {
            return link => link.ConferenceId == _conferenceId;
        }
    }
}
