import { PayloadAction } from '@reduxjs/toolkit';
import { put, select, takeEvery } from 'redux-saga/effects';
import { RootState } from 'src/store';
import { showMessage } from 'src/store/notifier/actions';
import { takeEverySynchronizedObjectChange } from 'src/store/saga-utils';
import { EQUIPMENT, SynchronizedEquipment } from 'src/store/signal/synchronization/synchronized-object-ids';
import { ProducerDevices } from 'src/store/webrtc/types';
import { StriveSettings, setCurrentDevice } from './reducer';
import { fetchDevices } from './thunks';
import { InputDeviceDto } from './types';
import i18next from 'i18next';

/**
 * Executed when local devices changed. Check if the selected devices are still available
 */
function* updateLocalDevices({ payload }: PayloadAction<InputDeviceDto[]>) {
   const deviceSettings: StriveSettings = yield select((state: RootState) => state.settings.obj);

   for (const producerDevice of ProducerDevices) {
      const device = deviceSettings[producerDevice].device;
      if (!device) continue;

      if (device.type === 'local') {
         if (!payload.find((x) => x.deviceId === device.deviceId)) {
            yield put(setCurrentDevice({ source: producerDevice, device: undefined }));

            yield put(
               showMessage({
                  type: 'info',
                  message: i18next.t('conference.notifications.device_disconnected_local', { name: producerDevice }),
               }),
            );
         }
      }
   }
}

/**
 * Executed when equipment changed. Check if the selected devices are still available
 */
function* updateEquipment() {
   const equipment: SynchronizedEquipment | null = yield select((state: RootState) => state.media.equipment);
   const deviceSettings: StriveSettings = yield select((state: RootState) => state.settings.obj);

   for (const producerDevice of ProducerDevices) {
      const device = deviceSettings[producerDevice].device;
      if (!device) continue;

      if (device.type === 'equipment') {
         if (!equipment?.connections[device.connectionId]?.devices.find((x) => x.deviceId === device.deviceId)) {
            yield put(setCurrentDevice({ source: producerDevice, device: undefined }));

            yield put(
               showMessage({
                  type: 'info',
                  message: i18next.t('conference.notifications.device_disconnected_equipment', {
                     name: producerDevice,
                  }),
               }),
            );
         }
      }
   }
}

function* mySaga() {
   yield takeEvery(fetchDevices.fulfilled, updateLocalDevices);
   yield takeEverySynchronizedObjectChange(EQUIPMENT, updateEquipment);
}

export default mySaga;
