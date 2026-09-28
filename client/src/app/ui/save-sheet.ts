import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { SignInService } from '../core/sign-in.service';
import { Icon } from './icon';

@Component({
  selector: 'app-save-sheet',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'ion-page' },
  template: `
    <div class="sheet-scroll">
      <div class="sheet-body">
        <span class="badge"><app-icon name="bookmark" [size]="26" /></span>
        <h1>לשמור את ההפתעה?</h1>
        <p class="lead">שמרנו את הרעיון. התחברות תשמור אותו גם בטלפון אחר, ונזכיר לך 3 ימים לפני האירוע.</p>
        <div class="stack">
          @if (signIn.isAvailable('apple')) { <button class="btn btn-dark btn-block" (click)="go('apple')" [disabled]="busy()">המשך עם Apple</button> }
          @if (signIn.isAvailable('google')) { <button class="btn btn-block google" (click)="go('google')" [disabled]="busy()">המשך עם Google</button> }
          @if (signIn.isAvailable('dev')) { <button class="btn btn-block" (click)="go('dev')" [disabled]="busy()">כניסת הדגמה</button> }
        </div>
        <p class="small muted center">בלי טופס ובלי סיסמה. אנחנו שומרים רק כינוי (״אשתי״), טווח גיל ותאריך – לא שם מלא, טלפון או כתובת.</p>
        <button class="btn btn-ghost" (click)="close()">לא עכשיו</button>
      </div>
    </div>
  `,
  styles: `
    .sheet-body { padding: 28px var(--gutter) 32px; display: flex; flex-direction: column; align-items: center; gap: 16px; text-align: center; }
    .badge { width: 60px; height: 60px; border-radius: 18px; background: var(--accent-soft); color: var(--accent); display: flex; align-items: center; justify-content: center; }
    h1 { font-size: 26px; }
    .stack { width: 100%; }
    .google { background: #fff; box-shadow: var(--shadow-sm); }
    .center { text-align: center; }
  `
})
export class SaveSheet {
  protected readonly signIn = inject(SignInService);
  private readonly modalCtrl = inject(ModalController);
  protected readonly busy = signal(false);

  async go(p: 'apple' | 'google' | 'dev') {
    this.busy.set(true);
    try {
      await this.signIn.signIn(p);
      await this.modalCtrl.dismiss(true);
    } catch {
      this.busy.set(false);
    }
  }

  close() { void this.modalCtrl.dismiss(false); }
}
