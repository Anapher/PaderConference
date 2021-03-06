using System.Collections.Generic;
using MediatR;

namespace Strive.Core.Services.Scenes.Requests
{
    public record FetchAvailableScenesRequest
        (string ConferenceId, string RoomId, IReadOnlyList<IScene> SceneStack) : IRequest<IReadOnlyList<IScene>>;
}
