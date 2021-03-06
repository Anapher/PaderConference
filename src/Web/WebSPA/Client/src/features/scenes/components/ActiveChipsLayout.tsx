import { makeStyles } from '@material-ui/core';
import React from 'react';
import ActiveParticipantsChips from './ActiveParticipantsChips';
import clsx from 'classnames';

export const ACTIVE_CHIPS_LAYOUT_HEIGHT = 40;

const useStyles = makeStyles((theme) => ({
   root: {
      display: 'flex',
      flexDirection: 'column',
   },
   chips: {
      marginTop: theme.spacing(1),
      marginBottom: theme.spacing(1),
      paddingRight: theme.spacing(2),
      height: 24,
   },
   content: {
      flex: 1,
      minHeight: 0,
   },
}));

type Props = {
   children?: React.ReactNode;
   className?: string;
   style?: React.CSSProperties;
   contentClassName?: string;
};

export default function ActiveChipsLayout({ children, className, style, contentClassName }: Props) {
   const classes = useStyles();

   return (
      <div className={clsx(classes.root, className)} style={style}>
         <ActiveParticipantsChips className={classes.chips} />
         <div className={clsx(classes.content, contentClassName)}>{children}</div>
      </div>
   );
}
