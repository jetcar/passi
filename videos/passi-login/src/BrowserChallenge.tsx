import {interpolate, spring, useCurrentFrame, useVideoConfig} from 'remotion';

export const BrowserChallenge = () => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const entrance = spring({frame, fps, config: {damping: 18, stiffness: 110}});
  const pulse = interpolate(frame % 54, [0, 27, 54], [0.9, 1.08, 0.9]);

  return (
    <div
      style={{
        background: '#f8fbff',
        borderRadius: 28,
        boxShadow: '0 35px 100px rgba(0,0,0,0.34)',
        height: 680,
        opacity: entrance,
        overflow: 'hidden',
        transform: `translateY(${(1 - entrance) * 60}px) scale(${0.94 + entrance * 0.06})`,
        width: 1160,
      }}
    >
      <div style={{alignItems: 'center', background: '#f4f7fb', borderBottom: '1px solid #dbe3ee', display: 'flex', gap: 14, height: 74, padding: '0 24px'}}>
        <div style={{background: '#ff5f57', borderRadius: 20, height: 16, width: 16}} />
        <div style={{background: '#febc2e', borderRadius: 20, height: 16, width: 16}} />
        <div style={{background: '#28c840', borderRadius: 20, height: 16, width: 16}} />
        <div style={{background: 'white', border: '1px solid #d7e0ea', borderRadius: 12, color: '#526274', fontSize: 20, marginLeft: 20, padding: '12px 24px', width: 770}}>
          🔒 passi.cloud/openidc/login
        </div>
      </div>

      <div style={{alignItems: 'center', display: 'flex', height: 606, justifyContent: 'center', padding: '0 70px'}}>
        <div style={{width: '54%'}}>
          <div style={{color: '#10243e', fontSize: 48, fontWeight: 800, letterSpacing: -1.5}}>Check your phone</div>
          <div style={{color: '#667589', fontSize: 23, lineHeight: 1.5, marginTop: 16}}>
            Open Passi on a registered device and approve this login request.
          </div>
          <div style={{background: 'white', border: '1px solid #dce5ef', borderRadius: 18, marginTop: 34, padding: '22px 24px'}}>
            <div style={{color: '#778497', fontSize: 16, fontWeight: 700}}>AVAILABLE DEVICE</div>
            <div style={{alignItems: 'center', color: '#152943', display: 'flex', fontSize: 22, fontWeight: 700, gap: 12, marginTop: 12}}>
              <span>📱</span> Alex’s Android
              <span style={{background: '#dff7e7', borderRadius: 20, color: '#168344', fontSize: 14, marginLeft: 'auto', padding: '6px 10px'}}>ONLINE</span>
            </div>
          </div>
          <div style={{color: '#8491a2', fontSize: 17, marginTop: 24}}>The browser will continue automatically.</div>
        </div>

        <div style={{alignItems: 'center', display: 'flex', flex: 1, flexDirection: 'column'}}>
          <div style={{color: '#46617d', fontSize: 20, fontWeight: 700}}>TAP THIS NUMBER IN PASSI</div>
          <div
            style={{
              alignItems: 'center',
              background: '#2196f3',
              border: '12px solid #d8eeff',
              borderRadius: 200,
              boxShadow: '0 20px 55px rgba(33,150,243,0.34)',
              color: 'white',
              display: 'flex',
              fontSize: 108,
              fontWeight: 900,
              height: 250,
              justifyContent: 'center',
              marginTop: 34,
              transform: `scale(${pulse})`,
              width: 250,
            }}
          >
            42
          </div>
          <div style={{color: '#728197', fontSize: 17, marginTop: 28}}>Request expires in 00:54</div>
        </div>
      </div>
    </div>
  );
};
