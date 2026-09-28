import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { AuthService } from './auth';

interface GpConfig { googleClientId: string; appleClientId: string; appleRedirectUri: string; devSignIn: boolean; }

declare global {
  interface Window {
    GP_CONFIG?: Partial<GpConfig>;
    google?: { accounts: { id: { initialize(o: object): void; prompt(cb?: (n: { isNotDisplayed(): boolean; isSkippedMoment(): boolean }) => void): void } } };
    AppleID?: { auth: { init(o: object): void; signIn(): Promise<{ authorization: { id_token: string } }> } };
  }
}

export type Provider = 'google' | 'apple' | 'dev';

/**
 * Gets an ID token from Google / Apple in the browser and exchanges it for our JWT.
 * The server verifies the token signature; the client never sees provider secrets.
 * On native builds swap this for a Capacitor social-login plugin — the server contract stays the same.
 */
@Injectable({ providedIn: 'root' })
export class SignInService {
  private readonly doc = inject(DOCUMENT);
  private readonly auth = inject(AuthService);
  private readonly config: GpConfig = {
    googleClientId: '', appleClientId: '', appleRedirectUri: '', devSignIn: false,
    ...(this.doc.defaultView?.GP_CONFIG ?? {})
  };

  isAvailable(p: Provider): boolean {
    if (p === 'google') return !!this.config.googleClientId;
    if (p === 'apple') return !!this.config.appleClientId;
    return this.config.devSignIn;
  }

  async signIn(p: Provider, devName = 'אורח/ת'): Promise<void> {
    if (p === 'dev') {
      const id = `demo-${Math.random().toString(36).slice(2, 10)}`;
      await this.auth.signIn('dev', `dev:${id}|${devName}`);
      return;
    }
    const token = p === 'google' ? await this.googleIdToken() : await this.appleIdToken();
    await this.auth.signIn(p, token);
  }

  private async googleIdToken(): Promise<string> {
    await this.loadScript('https://accounts.google.com/gsi/client');
    return new Promise<string>((resolve, reject) => {
      this.doc.defaultView!.google!.accounts.id.initialize({
        client_id: this.config.googleClientId,
        callback: (r: { credential?: string }) => (r.credential ? resolve(r.credential) : reject(new Error('cancelled'))),
        ux_mode: 'popup'
      });
      this.doc.defaultView!.google!.accounts.id.prompt(n => {
        if (n.isNotDisplayed() || n.isSkippedMoment()) reject(new Error('cancelled'));
      });
    });
  }

  private async appleIdToken(): Promise<string> {
    await this.loadScript('https://appleid.cdn-apple.com/appleauth/static/jsapi/appleid/1/he_IL/appleid.auth.js');
    const apple = this.doc.defaultView!.AppleID!;
    apple.auth.init({ clientId: this.config.appleClientId, scope: 'name', redirectURI: this.config.appleRedirectUri, usePopup: true });
    const res = await apple.auth.signIn();
    return res.authorization.id_token;
  }

  private loadScript(src: string): Promise<void> {
    if (this.doc.querySelector(`script[src="${src}"]`)) return Promise.resolve();
    return new Promise((resolve, reject) => {
      const s = this.doc.createElement('script');
      s.src = src;
      s.async = true;
      s.onload = () => resolve();
      s.onerror = () => reject(new Error(`Failed to load ${src}`));
      this.doc.head.appendChild(s);
    });
  }
}
