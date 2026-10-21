import {interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

const blue = '#2196f3';

export const BrowserLogin = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const entrance = spring({frame, fps, config: {damping: 18, stiffness: 110}});
  const typedLength = Math.floor(interpolate(frame, [35, 80], [0, 16], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  }));
  const username = 'alex@example.com'.slice(0, typedLength);
  const buttonScale = interpolate(frame, [95, 100, 106], [1, 0.94, 1], {
    extrapolateLeft: 'clamp',
    extrapolateRight: 'clamp',
  });

  return (
    <div
      style={{
        background: '#fff',
        border: '1px solid rgba(255,255,255,0.28)',
        borderRadius: 28,
        boxShadow: '0 35px 100px rgba(0,0,0,0.34)',
        height: 680,
        opacity: entrance,
        overflow: 'hidden',
        transform: `translateY(${(1 - entrance) * 60}px) scale(${0.94 + entrance * 0.06})`,
        width: 1160,
      }}
    >
      <div
        style={{
          alignItems: 'center',
          background: '#f4f7fb',
          borderBottom: '1px solid #dbe3ee',
          display: 'flex',
          gap: 14,
          height: 74,
          padding: '0 24px',
        }}
      >
        <div style={{background: '#ff5f57', borderRadius: 20, height: 16, width: 16}} />
        <div style={{background: '#febc2e', borderRadius: 20, height: 16, width: 16}} />
        <div style={{background: '#28c840', borderRadius: 20, height: 16, width: 16}} />
        <div
          style={{
            background: 'white',
            border: '1px solid #d7e0ea',
            borderRadius: 12,
            color: '#526274',
            fontSize: 20,
            marginLeft: 20,
            padding: '12px 24px',
            width: 770,
          }}
        >
          🔒 passi.cloud/openidc/login
        </div>
      </div>

      <div style={{background: '#f8fbff', display: 'flex', height: 606}}>
        <div style={{padding: '92px 74px', width: '54%'}}>
          <div style={{color: '#10243e', fontSize: 52, fontWeight: 800, letterSpacing: -2}}>Welcome back</div>
          <div style={{color: '#6a7889', fontSize: 24, lineHeight: 1.45, marginTop: 12}}>
            Enter your Passi username to continue securely.
          </div>
          <div style={{color: '#43566d', fontSize: 18, fontWeight: 700, marginTop: 48}}>USERNAME</div>
          <div
            style={{
              alignItems: 'center',
              background: 'white',
              border: `2px solid ${frame >= 25 ? blue : '#d6e0eb'}`,
              borderRadius: 14,
              color: '#152943',
              display: 'flex',
              fontSize: 25,
              height: 66,
              marginTop: 12,
              padding: '0 20px',
            }}
          >
            {username}
            {frame >= 25 && frame < 92 ? <span style={{color: blue, marginLeft: 2}}>|</span> : null}
          </div>
          <div
            style={{
              alignItems: 'center',
              background: blue,
              borderRadius: 14,
              color: 'white',
              display: 'flex',
              fontSize: 23,
              fontWeight: 750,
              height: 64,
              justifyContent: 'center',
              marginTop: 24,
              transform: `scale(${buttonScale})`,
            }}
          >
            Continue with Passi
          </div>
        </div>

        <div
          style={{
            alignItems: 'center',
            background: 'linear-gradient(145deg, #e8f4ff, #cfe8ff)',
            display: 'flex',
            flex: 1,
            justifyContent: 'center',
          }}
        >
          <div style={{position: 'relative'}}>
            <div
              style={{
                background: 'rgba(33,150,243,0.14)',
                borderRadius: 220,
                height: 330,
                left: -75,
                position: 'absolute',
                top: -68,
                width: 330,
              }}
            />
            <div style={{fontSize: 180, position: 'relative'}}>🔐</div>
            <div style={{color: '#174c7c', fontSize: 22, fontWeight: 700, marginTop: 50, textAlign: 'center'}}>
              Your password stays with you.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
