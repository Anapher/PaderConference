import React from 'react';
import { useSelector } from 'react-redux';
import useMyParticipantId from 'src/hooks/useMyParticipantId';
import { RootState } from 'src/store';
import { SynchronizedConferenceInfo } from 'src/store/signal/synchronization/synchronized-object-ids';
import ClassConference from './ClassConference';
import ConferenceNotOpen from './conference-not-open/ConferenceNotOpen';
import ConferenceNotOpenModerator from './conference-not-open/ConferenceNotOpenModerator';
import RequestUserInteractionView from './RequestUserInteractionView';

type Props = {
   conference: SynchronizedConferenceInfo;
};

export default function index({ conference }: Props) {
   const userInteractionMade = useSelector((state: RootState) => state.media.userInteractionMade);
   const myId = useMyParticipantId();
   const isModerator = conference.moderators.includes(myId);

   if (!conference.isOpen) {
      return isModerator ? (
         <ConferenceNotOpenModerator conferenceInfo={conference} />
      ) : (
         <ConferenceNotOpen conferenceInfo={conference} />
      );
   }

   if (!userInteractionMade) {
      return <RequestUserInteractionView />;
   }

   return <ClassConference />;
}
