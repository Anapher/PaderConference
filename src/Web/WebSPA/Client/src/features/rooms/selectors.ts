import { createSelector } from '@reduxjs/toolkit';
import _ from 'lodash';
import { RootState } from 'src/store';
import { createArrayEqualSelector } from 'src/utils/reselect';
import { selectMyParticipantId } from '../auth/selectors';
import { RoomViewModel } from './types';

export const selectRooms = (state: RootState) => state.rooms.synchronized;

export const selectParticipantRoom = (state: RootState) => {
   const myId = selectMyParticipantId(state);
   if (!myId) return undefined;

   const rooms = selectRooms(state);
   return rooms?.participants[myId];
};

export const selectParticipantsOfCurrentRoom = createArrayEqualSelector(
   createSelector(selectParticipantRoom, selectRooms, (room, rooms) => {
      if (!rooms) return [];

      return Object.entries(rooms.participants)
         .filter(([, roomId]) => roomId === room)
         .map<string>(([participantId]) => participantId);
   }),
   (x) => x,
);

export const selectIsParticipantInSameRoomAsMe = (state: RootState, otherParticipantId: string) =>
   selectParticipantsOfCurrentRoom(state).includes(otherParticipantId);

export const selectParticipantsOfCurrentRoomWithoutMe = createSelector(
   selectParticipantsOfCurrentRoom,
   selectMyParticipantId,
   (participants, participantId) => {
      return participants.filter((x) => x !== participantId);
   },
);

export const selectRoomViewModels = createSelector(selectRooms, (state) => {
   if (!state) return undefined;

   const { defaultRoomId, participants, rooms } = state;

   return _.sortBy(
      rooms.map<RoomViewModel>((room) => ({
         ...room,
         isDefaultRoom: defaultRoomId === room.roomId,
         participants: Object.entries(participants)
            .filter(([, roomId]) => roomId === room.roomId)
            .map(([participantId]) => participantId),
      })),
      (x) => x.isDefaultRoom,
      (x) => x.displayName,
   );
});
