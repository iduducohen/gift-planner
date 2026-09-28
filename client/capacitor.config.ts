import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'app.giftplanner.rega',
  appName: 'רגע',
  webDir: 'dist/client/browser',
  server: { androidScheme: 'https' }
};

export default config;
