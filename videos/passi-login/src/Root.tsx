import {Composition} from 'remotion';
import {PassiLoginVideo} from './PassiLoginVideo';

export const RemotionRoot = () => (
  <Composition
    id="PassiLogin"
    component={PassiLoginVideo}
    durationInFrames={1200}
    fps={30}
    width={1920}
    height={1080}
  />
);
