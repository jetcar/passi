import {spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const SuccessScene = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const pop = spring({frame: frame - 12, fps, config: {damping: 12, stiffness: 140}});

  return (
    <div style={{alignItems: 'center', display: 'flex', flexDirection: 'column'}}>
      <div
        style={{
          alignItems: 'center',
          background: '#2bd576',
          border: '14px solid rgba(43,213,118,0.18)',
          borderRadius: 160,
          boxShadow: '0 24px 80px rgba(43,213,118,0.28)',
          color: '#07170f',
          display: 'flex',
          fontSize: 112,
          fontWeight: 900,
          height: 220,
          justifyContent: 'center',
          transform: `scale(${pop})`,
          width: 220,
        }}
      >
        ✓
      </div>
      <div style={{fontSize: 76, fontWeight: 850, letterSpacing: -3, marginTop: 48}}>You’re signed in.</div>
      <div style={{color: '#c5d8ec', fontSize: 30, marginTop: 18}}>No password entered. No code copied.</div>
      <div style={{alignItems: 'center', display: 'flex', gap: 18, marginTop: 48}}>
        <div style={{background: 'rgba(255,255,255,0.1)', borderRadius: 18, fontSize: 21, padding: '17px 24px'}}>Browser request</div>
        <div style={{color: '#72bdff', fontSize: 30}}>→</div>
        <div style={{background: 'rgba(255,255,255,0.1)', borderRadius: 18, fontSize: 21, padding: '17px 24px'}}>Passi approval</div>
        <div style={{color: '#72bdff', fontSize: 30}}>→</div>
        <div style={{background: '#2196f3', borderRadius: 18, fontSize: 21, fontWeight: 750, padding: '17px 24px'}}>Secure access</div>
      </div>
    </div>
  );
};
