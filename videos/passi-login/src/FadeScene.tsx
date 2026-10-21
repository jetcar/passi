import type {ReactNode} from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';

export const FadeScene = ({children, duration}: {children: ReactNode; duration: number}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 15, duration - 18, duration], [0, 1, 1, 0], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return <AbsoluteFill style={{alignItems: 'center', justifyContent: 'center', opacity}}>{children}</AbsoluteFill>;
};
