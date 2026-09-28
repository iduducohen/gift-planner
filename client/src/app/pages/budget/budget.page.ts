import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { IonBackButton, IonContent, IonRange } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BriefStore, EMOTIONS } from '../../core/brief.store';
import { pronouns } from '../../core/format';
import { IdeaActions } from '../../core/idea-actions.service';
import { EmotionKey } from '../../core/models';
import { Icon } from '../../ui/icon';
import { Steps } from '../../ui/steps';

const PRESETS: [string, number][] = [['עד 100', 100], ['250', 250], ['500', 500], ['700', 700], ['1,000', 1000], ['2,000+', 2000]];

const FEEL_LABELS: Record<'f' | 'm' | 'n', Record<EmotionKey, string>> = {
  f: { moved: 'שתתרגש', surprised: 'שתופתע', pampered: 'שתתפנק', thrilled: 'שתתלהב', laugh: 'שתצחק', adventure: 'הרפתקה' },
  m: { moved: 'שיתרגש', surprised: 'שיופתע', pampered: 'שיתפנק', thrilled: 'שיתלהב', laugh: 'שיצחק', adventure: 'הרפתקה' },
  n: { moved: 'התרגשות', surprised: 'הפתעה', pampered: 'פינוק', thrilled: 'התלהבות', laugh: 'צחוק', adventure: 'הרפתקה' }
};

@Component({
  selector: 'app-budget',
  imports: [IonContent, IonBackButton, IonRange, Icon, Steps],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow">
        <div class="between">
          <ion-back-button defaultHref="/new/about" text="" class="icon-btn" aria-label="חזרה" />
          <app-steps [current]="3" />
        </div>

        <section class="stack">
          <h1>כמה תרצה להוציא?</h1>
          <div class="card amount">
            <span class="num">{{ d().budget >= 3000 ? '3,000+' : d().budget.toLocaleString('he-IL') }} ₪</span>
            <ion-range [min]="50" [max]="3000" [step]="50" [value]="d().budget" aria-label="תקציב"
                       (ionInput)="setBudget($any($event).detail.value)" />
            <div class="chips">
              @for (p of presets; track p[1]) {
                <button type="button" class="chip" [attr.aria-pressed]="d().budget === p[1]" (click)="setBudget(p[1])">{{ p[0] }}</button>
              }
            </div>
          </div>
        </section>

        <section class="stack">
          <div class="between">
            <h2>{{ feelQuestion() }}</h2>
            <span class="small muted">עד שתי בחירות</span>
          </div>
          <div class="feels">
            @for (e of emotions; track e.key) {
              <button type="button" class="feel" [attr.aria-pressed]="d().emotions.includes(e.key)" (click)="brief.toggleEmotion(e.key)">
                <app-icon [name]="e.icon" [size]="22" />{{ feelLabel(e.key) }}
              </button>
            }
          </div>
        </section>

        @if (error()) { <p class="error" role="alert">{{ error() }}</p> }

        <button type="button" class="btn btn-primary btn-block" [disabled]="busy() || !brief.canSubmit()" (click)="submit()">
          {{ busy() ? 'מחפשים רעיונות…' : 'הראה לי רעיונות' }}
        </button>
      </main>

      @if (busy()) {
        <div class="thinking" role="status" aria-live="polite">
          <span class="spark"><app-icon name="sparkle" [size]="30" /></span>
          <b>מחפשים את הרגע הנכון…</b>
          <span class="muted">בודקים מעל 50 רעיונות מול מה שסיפרת</span>
        </div>
      }
    </ion-content>
  `,
  styles: `
    .amount { padding: 22px 20px; display: flex; flex-direction: column; gap: 10px; }
    .num { font-family: var(--font-head); font-size: 40px; font-weight: 600; text-align: center; }
    .feels { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    @media (min-width: 600px) { .feels { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
    .feel { display: flex; align-items: center; gap: 10px; min-height: 60px; padding: 0 16px; border-radius: 16px; border: 0;
      background: var(--surface); box-shadow: var(--shadow-sm); color: var(--ink); font-size: 16px; font-weight: 700; }
    .feel[aria-pressed='true'] { background: var(--accent-soft); color: var(--accent); box-shadow: inset 0 0 0 2px var(--accent); }
    .error { color: var(--accent); font-weight: 600; }
    .thinking { position: fixed; inset: 0; z-index: 30; background: rgba(251, 248, 244, .94); display: flex; flex-direction: column;
      align-items: center; justify-content: center; gap: 10px; font-size: 18px; }
    .spark { width: 68px; height: 68px; border-radius: 999px; background: var(--accent-soft); color: var(--accent); display: flex;
      align-items: center; justify-content: center; animation: pulse 1.2s ease-in-out infinite alternate; }
    @keyframes pulse { from { transform: scale(.92); } to { transform: scale(1.06); } }
  `
})
export class BudgetPage {
  protected readonly brief = inject(BriefStore);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly actions = inject(IdeaActions);

  protected readonly d = this.brief.draft;
  protected readonly presets = PRESETS;
  protected readonly emotions = EMOTIONS;
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly p = computed(() => pronouns(this.d().relationship?.key));
  private readonly gender = computed(() => (this.p().her === 'היא' ? 'f' : this.p().her === 'הוא' ? 'm' : 'n'));

  protected readonly feelQuestion = computed(() =>
    ({ f: 'מה תרצה שהיא תרגיש?', m: 'מה תרצה שהוא ירגיש?', n: 'מה תרצה שירגישו?' })[this.gender()]);

  feelLabel(k: EmotionKey) { return FEEL_LABELS[this.gender()][k]; }

  setBudget(v: number) { this.brief.patch({ budget: Math.max(0, Math.min(20000, Math.round(v))) }); }

  async submit() {
    this.busy.set(true);
    this.error.set(null);
    try {
      const session = await firstValueFrom(this.api.createSession(this.brief.toRequest()));
      if (session.question) {
        await this.router.navigate(['/session', session.id, 'clarify'], { state: { question: session.question } });
        return;
      }
      const set = await firstValueFrom(this.api.generate(session.id));
      await this.router.navigate(['/session', session.id, 'ideas', set.id], { state: { set } });
    } catch {
      this.error.set('משהו השתבש. בדקו את החיבור ונסו שוב.');
      await this.actions.toast('לא הצלחנו ליצור רעיונות');
    } finally {
      this.busy.set(false);
    }
  }
}
