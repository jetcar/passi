import type {ReactNode} from 'react';

export const PhoneFrame = ({children}: {children: ReactNode}) => (
  <div
    style={{
      background: '#090d13',
      border: '2px solid #354252',
      borderRadius: 58,
      boxShadow: '0 34px 90px rgba(0,0,0,0.45)',
      height: 820,
      padding: 14,
      position: 'relative',
      width: 410,
    }}
  >
    <div
      style={{
        background: '#111820',
        borderRadius: 50,
        height: '100%',
        overflow: 'hidden',
        position: 'relative',
        width: '100%',
      }}
    >
      <div
        style={{
          alignItems: 'center',
          color: '#dfe8f2',
          display: 'flex',
          fontFamily: 'Arial, sans-serif',
          fontSize: 15,
          fontWeight: 700,
          height: 42,
          justifyContent: 'space-between',
          left: 0,
          padding: '0 26px',
          position: 'absolute',
          right: 0,
          top: 0,
          zIndex: 5,
        }}
      >
        <span>10:42</span>
        <span>● ● ▰</span>
      </div>
      <div
        style={{
          background: '#05070a',
          borderRadius: 18,
          height: 27,
          left: '50%',
          position: 'absolute',
          top: 12,
          transform: 'translateX(-50%)',
          width: 114,
          zIndex: 6,
        }}
      />
      {children}
      <div
        style={{
          background: 'rgba(255,255,255,0.72)',
          borderRadius: 10,
          bottom: 9,
          height: 5,
          left: '50%',
          position: 'absolute',
          transform: 'translateX(-50%)',
          width: 130,
          zIndex: 6,
        }}
      />
    </div>
  </div>
);
