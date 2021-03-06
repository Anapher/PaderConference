import {
   ConsumerLayers,
   DtlsParameters,
   IceCandidate,
   IceParameters,
   ProducerOptions,
   RtpCapabilities,
   SctpCapabilities,
   SctpParameters,
} from 'mediasoup/lib/types';
import { ProducerSource } from '../types';

export type InitializeConnectionRequest = {
   connectionId: string;
   participantId: string;
   sctpCapabilities: SctpCapabilities;
   rtpCapabilities: RtpCapabilities;
};

export type CreateTransportRequest = {
   sctpCapabilities?: SctpCapabilities;
   forceTcp?: boolean;
   producing: boolean;
   consuming: boolean;
};

export type ConnectTransportRequest = {
   transportId: string;
   dtlsParameters: any;
};

export type TransportProduceRequest = {
   transportId: string;
} & ProducerOptions;

export type CreateTransportResponse = {
   id: string;
   iceParameters: IceParameters;
   iceCandidates: IceCandidate[];
   dtlsParameters: DtlsParameters;
   sctpParameters?: SctpParameters;
};

export type TransportProduceResponse = {
   id: string;
};

export type StreamType = 'producer' | 'consumer';
export type StreamAction = 'pause' | 'resume' | 'close';

export type ChangeStreamRequest = {
   id: string;
   type: 'producer' | 'consumer';
   action: StreamAction;
};

export type SetPreferredLayersRequest = {
   consumerId: string;
   layers: ConsumerLayers;
};

export type ChangeProducerSourceRequest = {
   source: ProducerSource;
   action: StreamAction;
};
