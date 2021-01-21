import { createSelector } from '@reduxjs/toolkit';
import { RootState } from 'src/store';
import { selectAccessToken } from '../auth/selectors';

export const selectParticipants = (state: RootState) => state.conference.participants;

export const selectParticipant = (state: RootState, participantId: string) =>
   selectParticipants(state)?.find((x) => x.participantId === participantId);

export const selectOtherParticipants = createSelector(selectParticipants, selectAccessToken, (participants, token) => {
   return participants?.filter((x) => x.participantId !== token?.nameid);
});

export const selectParticipantTempPermissions = (state: RootState, participantId: string) =>
   state.conference.tempPermissions?.[participantId];