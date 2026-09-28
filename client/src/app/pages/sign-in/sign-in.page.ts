import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { IonContent, NavController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth';
import { Dialogs } from '../../core/dialogs';
import { IdeaActions } from '../../core/idea-actions.service';
import { Provider, SignInService } from '../../core/sign-in.service';
import { Icon } from '../../ui/icon';

@Component({
  selector: 'app-sign-in',
  imports: [IonContent, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow center">
        <span class="logo"><app-icon name="gift" [size]="30" /></span>
        @if (!auth.isSignedIn()) {
          <h1>כל ההפתעות שלך, בכל מכשיר</h1>
          <p class="lead">התחברות שומרת את האנשים, הרעיונות והתוכניות – ומה שכבר עשית כאורח עובר איתך.</p>
          <div class="stack full">
            @if (signIn.isAvailable('apple')) { <button class="btn btn-dark btn-block" [disabled]="busy()" (click)="go('apple')">המשך עם Apple</button> }
            @if (signIn.isAvailable('google')) { <button class="btn btn-block google" [disabled]="busy()" (click)="go('google')">המשך עם Google</button> }
            @if (signIn.isAvailable('dev')) { <button class="btn btn-block" [disabled]="busy()" (click)="go('dev')">כניסת הדגמה (פיתוח)</button> }
          </div>
          <p class="small muted">בלי טופס ובלי סיסמה. שומרים רק כינויים, טווחי גיל ותאריכים.</p>
        } @else {
          <h1>שלום{{ auth.displayName() ? ', ' + auth.displayName() : '' }}</h1>
          <p class="lead">הנתונים שלך שמורים בחשבון.</p>
          <div class="stack full">
            <button class="btn btn-block" (click)="exportData()"><app-icon name="external" [size]="18" /> הורדת הנתונים שלי</button>
            <button class="btn btn-block" (click)="signOut()">יציאה</button>
            <button class="btn btn-ghost danger" (click)="deleteAccount()">מחיקת החשבון וכל הנתונים</button>
          </div>
        }
      </main>
    </ion-content>
  `,
  styles: `
    .center { align-items: center; text-align: center; padding-top: 56px; }
    .logo { width: 68px; height: 68px; border-radius: 20px; background: var(--accent); color: #fff; display: flex; align-items: center; justify-content: center; }
    .full { width: 100%; }
    .google { background: #fff; box-shadow: var(--shadow-sm); }
    .danger { color: var(--accent); }
  `
})
export class SignInPage {
  protected readonly auth = inject(AuthService);
  protected readonly signIn = inject(SignInService);
  private readonly api = inject(ApiService);
  private readonly nav = inject(NavController);
  private readonly dialogs = inject(Dialogs);
  private readonly actions = inject(IdeaActions);
  protected readonly busy = signal(false);

  async go(p: Provider) {
    this.busy.set(true);
    try {
      const name = p === 'dev' ? (await this.dialogs.askText('איך לקרוא לך?', 'לכניסת הדגמה בלבד', 'דודו')) ?? 'אורח/ת' : undefined;
      await this.signIn.signIn(p, name);
      await this.actions.toast('התחברת ✓');
      this.nav.navigateRoot('/');
    } catch {
      await this.actions.toast('ההתחברות לא הושלמה');
    } finally {
      this.busy.set(false);
    }
  }

  async exportData() {
    const data = await firstValueFrom(this.api.exportData());
    const url = URL.createObjectURL(new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' }));
    const a = Object.assign(document.createElement('a'), { href: url, download: 'my-gift-planner-data.json' });
    a.click();
    URL.revokeObjectURL(url);
  }

  signOut() {
    this.auth.signOut();
    this.nav.navigateRoot('/');
  }

  async deleteAccount() {
    if (!(await this.dialogs.confirm('למחוק הכול?', 'האנשים, הרעיונות והתוכניות יימחקו לצמיתות.', 'מחיקה', true))) return;
    await firstValueFrom(this.api.deleteAccount());
    this.auth.signOut();
    this.nav.navigateRoot('/');
  }
}
