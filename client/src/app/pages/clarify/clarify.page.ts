import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { IonBackButton, IonContent } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { QuestionDto } from '../../core/models';
import { Icon } from '../../ui/icon';

/** At most one clarifying question — asked only when the brief is thin or contradictory. */
@Component({
  selector: 'app-clarify',
  imports: [IonContent, IonBackButton, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow">
        <ion-back-button defaultHref="/new/budget" text="" class="icon-btn" aria-label="חזרה" />
        <div class="hint"><app-icon name="sparkle" [size]="18" /> שאלה אחת, והרעיונות יהיו מדויקים יותר</div>
        <h1>{{ question()?.text ?? 'מה ישמח יותר?' }}</h1>
        <div class="stack">
          @for (o of question()?.options ?? []; track o.key) {
            <button type="button" class="row-link opt" [disabled]="busy()" (click)="answer(o.key)">
              <span class="txt"><span class="v">{{ o.label }}</span><span class="k">{{ o.hint }}</span></span>
              <app-icon name="chevron" class="chev" />
            </button>
          }
        </div>
        <button type="button" class="btn btn-ghost" [disabled]="busy()" (click)="answer(null)">דלג, תפתיע אותי</button>
        @if (busy()) { <p class="muted center" role="status">מכינים רעיונות…</p> }
      </main>
    </ion-content>
  `,
  styles: `
    .hint { align-self: flex-start; display: inline-flex; align-items: center; gap: 8px; min-height: 36px; padding: 0 14px; border-radius: 999px; background: var(--accent-soft); color: var(--accent); font-size: 14px; font-weight: 700; }
    .opt { min-height: 84px; }
    .opt .v { font-size: 19px; }
    .center { text-align: center; }
  `
})
export class ClarifyPage {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  readonly sessionId = input.required<string>();
  protected readonly question = signal<QuestionDto | null>(
    (this.router.getCurrentNavigation()?.extras.state?.['question'] as QuestionDto | undefined) ?? history.state?.question ?? null);
  protected readonly busy = signal(false);

  async answer(key: string | null) {
    this.busy.set(true);
    try {
      await firstValueFrom(this.api.clarify(this.sessionId(), key));
      const set = await firstValueFrom(this.api.generate(this.sessionId()));
      await this.router.navigate(['/session', this.sessionId(), 'ideas', set.id], { state: { set }, replaceUrl: true });
    } finally {
      this.busy.set(false);
    }
  }
}
