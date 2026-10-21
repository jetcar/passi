import {Img, interpolate, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {PhoneFrame} from './PhoneFrame';

const numbers = [18, 42, 76];

export const PhoneChallenge = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const entrance = spring({frame, fps, config: {damping: 18}});
  const selected = frame >= 105;
  const biometric = frame >= 145;
  const approved = frame >= 205;
  const tapScale = interpolate(frame, [96, 104, 112], [1, 0.86, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div style={{transform: `translateY(${(1 - entrance) * 80}px) scale(${0.94 + entrance * 0.06})`}}>
      <PhoneFrame>
        <div
          style={{
            background: '#fafafa',
            color: '#111827',
            fontFamily: 'Arial, sans-serif',
            height: '100%',
            padding: '76px 26px 32px',
          }}
        >
          <div style={{alignItems: 'center', display: 'flex', gap: 14}}>
            <Img src={staticFile('passi-icon.png')} style={{borderRadius: 14, height: 52, width: 52}} />
            <div>
              <div style={{fontSize: 22, fontWeight: 800}}>Approve login</div>
              <div style={{color: '#687485', fontSize: 15}}>passi.cloud</div>
            </div>
          </div>

          <div style={{color: '#5d6876', fontSize: 15, marginTop: 42, textAlign: 'center'}}>Request expires in</div>
          <div style={{fontSize: 28, fontWeight: 800, marginTop: 4, textAlign: 'center'}}>00:54</div>
          <div style={{fontSize: 18, fontWeight: 750, marginTop: 32, textAlign: 'center'}}>Tap the number shown in your browser</div>

          <div style={{display: 'flex', gap: 18, justifyContent: 'center', marginTop: 34}}>
            {numbers.map((number) => {
              const isTarget = number === 42;
              return (
                <div
                  key={number}
                  style={{
                    alignItems: 'center',
                    background: selected && isTarget ? '#2196f3' : 'white',
                    border: `3px solid ${selected && isTarget ? '#2196f3' : '#162235'}`,
                    borderRadius: 80,
                    color: selected && isTarget ? 'white' : '#162235',
                    display: 'flex',
                    fontSize: 28,
                    fontWeight: 800,
                    height: 78,
                    justifyContent: 'center',
                    transform: isTarget ? `scale(${tapScale})` : undefined,
                    width: 78,
                  }}
                >
                  {number}
                </div>
              );
            })}
          </div>

          <div style={{color: '#6b7280', fontSize: 15, lineHeight: 1.5, marginTop: 38, textAlign: 'center'}}>
            This verifies that you are approving the same login shown on your computer.
          </div>
          <div style={{background: '#edf2f7', borderRadius: 18, color: '#334155', fontSize: 17, marginTop: 36, padding: 18, textAlign: 'center'}}>
            alex@example.com
          </div>
        </div>

        {biometric ? (
          <div
            style={{
              alignItems: 'center',
              background: 'rgba(3,8,15,0.72)',
              bottom: 0,
              display: 'flex',
              justifyContent: 'center',
              left: 0,
              position: 'absolute',
              right: 0,
              top: 0,
            }}
          >
            <div style={{background: 'white', borderRadius: 28, color: '#111827', padding: '34px 28px', textAlign: 'center', width: 300}}>
              <div style={{fontSize: 62}}>{approved ? '✓' : '◉'}</div>
              <div style={{fontSize: 22, fontWeight: 800, marginTop: 12}}>{approved ? 'Approved' : 'Confirm it’s you'}</div>
              <div style={{color: '#667085', fontSize: 16, lineHeight: 1.45, marginTop: 10}}>
                {approved ? 'Secure login confirmed' : 'Use your fingerprint to approve this login'}
              </div>
            </div>
          </div>
        ) : null}
      </PhoneFrame>
    </div>
  );
};
