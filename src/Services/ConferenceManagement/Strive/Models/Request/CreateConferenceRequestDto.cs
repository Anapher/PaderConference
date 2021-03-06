#pragma warning disable 8618
// this file is validated so the nullable annotations are enforced

using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Strive.Core.Domain.Entities;

namespace Strive.Models.Request
{
    public class CreateConferenceRequestDto
    {
        public ConferenceConfiguration Configuration { get; set; }

        public Dictionary<PermissionType, Dictionary<string, JValue>>? Permissions { get; set; }
    }
}
