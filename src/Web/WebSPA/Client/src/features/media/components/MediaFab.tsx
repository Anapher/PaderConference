import { Fab, Tooltip, useTheme } from '@material-ui/core';
import React from 'react';
import { useTranslation } from 'react-i18next';
import { useDispatch } from 'react-redux';
import { AnimatedIconProps } from 'src/assets/animated-icons/AnimatedIconBase';
import { showMessage } from 'src/store/notifier/actions';
import { UseMediaState } from 'src/store/webrtc/hooks/useMedia';
import { formatErrorMessage } from 'src/utils/error-utils';

type Props = {
   className?: string;
   control: UseMediaState;
   pauseOnToggle?: boolean;
   translationKey: string;

   Icon: React.ComponentType<AnimatedIconProps>;
   [x: string]: any;
} & Omit<React.ComponentProps<typeof Fab>, 'children'>;

export default function MediaFab({
   Icon,
   pauseOnToggle,
   control: { enable, disable, pause, resume, enabled, paused },
   translationKey,
   ...fabProps
}: Props) {
   const dispatch = useDispatch();
   const theme = useTheme();
   const { t } = useTranslation();

   const handleClick = async () => {
      try {
         if (!enabled) {
            await enable();
         } else {
            if (paused) {
               await resume();
            } else {
               if (pauseOnToggle) {
                  await pause();
               } else {
                  await disable();
               }
            }
         }
      } catch (error) {
         if (error.message) {
            dispatch(showMessage({ message: formatErrorMessage(error), type: 'error' }));
         } else {
            dispatch(showMessage({ message: error?.toString(), type: 'error' }));
         }
      }
   };

   let title: string;
   if (enabled) {
      if (paused) title = t(`conference.media.controls.${translationKey}.paused`);
      else title = t(`conference.media.controls.${translationKey}.active`);
   } else {
      title = t(`conference.media.controls.${translationKey}.disabled`);
   }

   const label =
      enabled && !paused
         ? t(`conference.media.controls.${translationKey}.label_disable`)
         : t(`conference.media.controls.${translationKey}.label_enable`);

   return (
      <Tooltip title={title} aria-label={label} arrow>
         <Fab
            id={`media-controls-${translationKey}`}
            color={enabled ? 'primary' : 'default'}
            onClick={handleClick}
            {...fabProps}
         >
            <Icon
               activated={enabled && !paused}
               color={enabled ? theme.palette.primary.contrastText : theme.palette.background.default}
               width={24}
               height={24}
            />
         </Fab>
      </Tooltip>
   );
}
