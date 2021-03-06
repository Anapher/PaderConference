using Strive.Core.Dto;
using Strive.Core.Errors;

namespace Strive.Core.Services.BreakoutRooms
{
    public class BreakoutRoomError : ErrorsProvider<ServiceErrorCode>
    {
        public static Error AlreadyOpen =>
            BadRequest("Cannot open breakout rooms as they are already open. Please close them first.",
                ServiceErrorCode.BreakoutRoom_AlreadyOpen);

        public static Error NotOpen =>
            new BadRequestError<ServiceErrorCode>("Breakout rooms are not open.",
                ServiceErrorCode.BreakoutRoom_NotOpen);

        public static Error AssigningParticipantsFailed =>
            InternalServerError("Assigning participants failed.",
                ServiceErrorCode.BreakoutRoom_AssigningParticipantsFailed);
    }
}
