import {Img, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';

export const OutroScene = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const rise = spring({frame, fps, config: {damping: 18}});

  return (
    <div style={{alignItems: 'center', display: 'flex', flexDirection: 'column', transform: `translateY(${(1 - rise) * 45}px)`}}>
      <Img src={staticFile('passi-icon.png')} style={{borderRadius: 38, height: 138, width: 138}} />
      <div style={{fontSize: 88, fontWeight: 850, letterSpacing: -4, marginTop: 28}}>Passi</div>
      <div style={{color: '#c7e3ff', fontSize: 35, marginTop: 12}}>Passwordless authentication that feels effortless.</div>
      <div style={{display: 'flex', gap: 18, marginTop: 48}}>
        {['Number matching', 'Biometric approval', 'OAuth2 ready'].map((label) => (
          <div key={label} style={{background: 'rgba(255,255,255,0.1)', border: '1px solid rgba(255,255,255,0.16)', borderRadius: 30, fontSize: 19, padding: '13px 22px'}}>
            {label}
          </div>
        ))}
      </div>
      <div style={{color: '#78bfff', fontSize: 26, fontWeight: 800, marginTop: 58}}>PASSI.CLOUD</div>
    </div>
  );
};
