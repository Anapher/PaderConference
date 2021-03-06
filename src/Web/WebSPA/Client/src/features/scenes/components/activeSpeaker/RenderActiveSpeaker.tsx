import { makeStyles } from '@material-ui/core';
import clsx from 'classnames';
import React from 'react';
import { Size } from 'src/types';
import { expandToBox } from '../../calculations';
import { ActiveSpeakerScene, RenderSceneProps } from '../../types';
import useSomeParticipants from '../../useSomeParticipants';
import ParticipantTile from '../ParticipantTile';

const useStyles = makeStyles({
   root: {
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
   },
   rootCenter: {
      justifyContent: 'center',
   },
});

const getListeningParticipantsWidth = (width: number) => {
   if (width <= 400) return 100;
   if (width <= 800) return 180;
   if (width <= 1200) return 260;

   return 340;
};

export default function RenderActiveSpeaker({ className, dimensions }: RenderSceneProps<ActiveSpeakerScene>) {
   const classes = useStyles();

   const tileWidth = getListeningParticipantsWidth(dimensions.width);
   const tileHeight = (tileWidth / 16) * 9;

   const activeParticipantDimensions: Size = {
      width: dimensions.width - 16,
      height: dimensions.height - 8 - 8 - 16 - tileHeight,
   };

   const size = expandToBox({ height: 9, width: 16 }, activeParticipantDimensions);
   const smallTileCount = (dimensions.width - 8) / (tileWidth + 8);

   const activeParticipants = useSomeParticipants(smallTileCount);

   if (activeParticipants.length === 0) return null;

   return (
      <div className={clsx(className, classes.root, activeParticipants.length === 1 && classes.rootCenter)}>
         <div style={{ margin: 8, ...size }}>
            <ParticipantTile key={activeParticipants[0].id} {...size} participant={activeParticipants[0]} />
         </div>
         <div style={{ display: 'flex', marginTop: 8 }}>
            {activeParticipants.slice(1).map((participant, i) => (
               <div style={{ width: tileWidth, height: tileHeight, marginLeft: i === 0 ? 0 : 16 }} key={participant.id}>
                  <ParticipantTile width={tileWidth} height={tileHeight} participant={participant} />
               </div>
            ))}
         </div>
      </div>
   );
}
