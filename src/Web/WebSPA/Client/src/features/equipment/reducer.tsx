import { createSlice, PayloadAction } from '@reduxjs/toolkit';
import { DomainError, SuccessOrError } from 'src/communication-types';
import { events } from 'src/equipment-hub';
import { EquipmentCommand } from 'src/equipment-hub.types';
import { onEventOccurred } from 'src/store/signal/actions';
import { EquipmentCommandResult } from './types';
import * as equipmentHub from 'src/equipment-hub';

const MAX_HISTORY_LEN = 128;
let idCounter = 0;

type EquipmentState = {
   commandHistory: EquipmentCommandResult[];
   initializeError: DomainError | null;
};

const initialState: EquipmentState = {
   commandHistory: [],
   initializeError: null,
};

const equipmentSlice = createSlice({
   name: 'equipment',
   initialState,
   reducers: {
      commandExecuted(state, { payload: { id, error } }: PayloadAction<{ error?: DomainError; id: number }>) {
         state.commandHistory = state.commandHistory.map((x) => (x.id === id ? { ...x, error, executed: true } : x));
      },
   },
   extraReducers: {
      [onEventOccurred(events.onEquipmentCommand).type]: (state, { payload }: PayloadAction<EquipmentCommand>) => {
         state.commandHistory = [
            { command: payload, id: idCounter++ },
            ...state.commandHistory.slice(0, MAX_HISTORY_LEN),
         ];
      },
      [equipmentHub.initialize.returnAction]: (state, { payload }: PayloadAction<SuccessOrError>) => {
         if (!payload.success) {
            state.initializeError = payload.error;
         }
      },
   },
});

export const { commandExecuted } = equipmentSlice.actions;

export default equipmentSlice.reducer;
