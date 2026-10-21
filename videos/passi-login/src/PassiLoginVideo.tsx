import {AbsoluteFill, Img, interpolate, Sequence, staticFile, useCurrentFrame} from 'remotion';
import {BrowserLogin} from './BrowserLogin';
import {PhoneChallenge} from './PhoneChallenge';

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
      <Sequence from={360} durationInFrames={270}>
        <div style={{alignItems: 'center', display: 'flex', gap: 110, height: '100%', justifyContent: 'center', width: '100%'}}>
          <div style={{maxWidth: 570}}>
            <div style={{color: '#78bfff', fontSize: 24, fontWeight: 800, letterSpacing: 3}}>STEP TWO</div>
            <div style={{fontSize: 66, fontWeight: 820, letterSpacing: -2, lineHeight: 1.05, marginTop: 18}}>
              Approve on your phone.
            </div>
            <div style={{color: '#c5d8ec', fontSize: 29, lineHeight: 1.45, marginTop: 28}}>
              Match the number, then confirm with your fingerprint or PIN.
            </div>
          </div>
          <PhoneChallenge />
        </div>
      </Sequence>
    </AbsoluteFill>
  );
};
