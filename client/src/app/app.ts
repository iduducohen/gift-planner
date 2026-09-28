import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { IonApp, IonRouterOutlet } from '@ionic/angular';
import { AuthService } from './core/auth';
import { Icon } from './ui/icon';

/**
 * Responsive shell: a top navigation bar on desktop, a bottom tab bar on mobile.
 * Pages render inside ion-router-outlet (native-feeling transitions on mobile).
 */
@Component({
  selector: 'app-root',
  imports: [IonApp, IonRouterOutlet, RouterLink, RouterLinkActive, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-app>
      <header class="topbar">
        <a routerLink="/" class="brand"><span class="logo"><app-icon name="gift" [size]="18" /></span>רגע</a>
        <nav aria-label="ניווט ראשי">
          @for (t of tabs; track t.path) {
            <a [routerLink]="t.path" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: t.path === '/' }">{{ t.label }}</a>
          }
        </nav>
        <a routerLink="/sign-in" class="account">
          <app-icon name="user" [size]="18" />{{ auth.isSignedIn() ? (auth.displayName() ?? 'החשבון שלי') : 'כניסה' }}
        </a>
      </header>

      <ion-router-outlet id="main" />

      <nav class="tabbar" aria-label="ניווט ראשי">
        @for (t of tabs; track t.path) {
          <a [routerLink]="t.path" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: t.path === '/' }">
            <app-icon [name]="t.icon" [size]="22" /><span>{{ t.label }}</span>
          </a>
        }
      </nav>
    </ion-app>
  `,
  styleUrl: './app.scss'
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly tabs = [
    { path: '/', label: 'מתנה חדשה', icon: 'gift' },
    { path: '/people', label: 'האנשים שלי', icon: 'people' },
    { path: '/saved', label: 'שמורים', icon: 'bookmark' }
  ];
}
