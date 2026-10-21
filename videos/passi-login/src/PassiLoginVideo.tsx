import {AbsoluteFill, Img, interpolate, Sequence, staticFile, useCurrentFrame} from 'remotion';
import {BrowserLogin} from './BrowserLogin';

export const PassiLoginVideo = () => {
  const frame = useCurrentFrame();
  const opacity = interpolate(frame, [0, 24], [0, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <AbsoluteFill
      style={{
        alignItems: 'center',
        background: 'linear-gradient(135deg, #07111f 0%, #0b2b4d 55%, #0d5aa7 100%)',
        color: 'white',
        display: 'flex',
        fontFamily: 'Arial, sans-serif',
        justifyContent: 'center',
      }}
    >
      <Sequence durationInFrames={120}>
        <div style={{alignItems: 'center', display: 'flex', flexDirection: 'column', opacity}}>
          <Img src={staticFile('passi-icon.png')} style={{borderRadius: 52, height: 180, width: 180}} />
          <div style={{fontSize: 112, fontWeight: 800, letterSpacing: -5, marginTop: 36}}>Passi</div>
          <div style={{color: '#c7e3ff', fontSize: 38, marginTop: 16}}>Passwordless login. One secure tap.</div>
        </div>
      </Sequence>
      <Sequence from={120} durationInFrames={240}>
        <BrowserLogin />
      </Sequence>
    </AbsoluteFill>
  );
};
