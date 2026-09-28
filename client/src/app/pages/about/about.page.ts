import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { IonBackButton, IonContent } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BriefStore } from '../../core/brief.store';
import { pronouns } from '../../core/format';
import { Pickers } from '../../core/pickers';
import { Icon } from '../../ui/icon';
import { Steps } from '../../ui/steps';

const ALREADY_HAS = ['בושם', 'תכשיטים', 'שעון', 'פרחים', 'ספא', 'שוקולד', 'יין', 'ספרים', 'כלי מטבח', 'נרות'];
const DISLIKES = ['אלכוהול', 'מתוקים', 'אקסטרים', 'בגדים', 'גאדג׳טים', 'עציצים', 'הופעות רועשות', 'גבהים'];

@Component({
  selector: 'app-about',
  imports: [IonContent, IonBackButton, Icon, Steps],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow">
        <div class="between">
          <ion-back-button defaultHref="/" text="" class="icon-btn back" aria-label="חזרה" />
          <app-steps [current]="2" />
        </div>

        <div class="stack-sm">
          <h1>ספר לי {{ p().about }}</h1>
          <p class="lead">כמה פרטים קטנים, והרעיונות יהיו מדויקים.</p>
        </div>

        <button type="button" class="row-link" (click)="pickAge()">
          <span class="ico warm"><app-icon name="user" [size]="22" /></span>
          <span class="txt"><span class="k">גיל</span>
            <span class="v" [class.placeholder]="!d().ageRange">{{ d().ageRange?.label ?? 'בחירת טווח גיל' }}</span></span>
          <app-icon name="chevron" class="chev" />
        </button>

        <section class="stack">
          <span class="label">מה {{ p().her }} {{ p().likes }}?</span>
          <div class="chips">
            @for (i of d().interests; track i) {
              <span class="chip chip-accent chip-remove">{{ i }}
                <button type="button" class="x" (click)="brief.toggleInterest(i)" [attr.aria-label]="'הסרת ' + i"><app-icon name="close" [size]="14" /></button>
              </span>
            }
            <button type="button" class="chip pick" (click)="pickInterests()">
              <app-icon name="plus" [size]="16" /> {{ d().interests.length ? 'עוד מהרשימה' : 'בחירה מתוך ' + (total() || '150+') + ' תחומים' }}
            </button>
          </div>
          @if (suggestions().length) {
            <div class="chips suggest">
              <span class="small muted">אולי גם:</span>
              @for (s of suggestions(); track s) {
                <button type="button" class="chip" (click)="brief.toggleInterest(s)">+ {{ s }}</button>
              }
            </div>
          }
        </section>

        <section class="card fold">
          <button type="button" class="fold-head" (click)="avoidOpen.set(!avoidOpen())" [attr.aria-expanded]="avoidOpen()">
            <span class="ico"><app-icon name="ban" [size]="20" /></span>
            <span class="txt"><b>מה לא להציע {{ p().toHer }}</b>
              <span class="small muted">{{ d().avoid.length ? d().avoid.join(' · ') : 'כבר ' + p().has + ', או שלא אוהב/ת' }}</span></span>
            <app-icon [name]="'chevron'" class="rot" [class.open]="avoidOpen()" />
          </button>
          @if (avoidOpen()) {
            <div class="fold-body">
              <p class="small muted">כדי שלא נציע משהו שכבר {{ p().has }} או שלא אוהבים. נשמר בפרופיל לפעמים הבאות.</p>
              <span class="label">כבר {{ p().has }} / כבר קיבל/ה</span>
              <div class="chips">
                @for (h of alreadyHas; track h) {
                  <button type="button" class="chip" [attr.aria-pressed]="isAvoided(h)" (click)="toggleAvoid(h)">{{ isAvoided(h) ? '✓ ' : '' }}{{ h }}</button>
                }
              </div>
              <span class="label">לא אוהב/ת</span>
              <div class="chips">
                @for (h of dislikes; track h) {
                  <button type="button" class="chip" [attr.aria-pressed]="isAvoided(h)" (click)="toggleAvoid(h)">{{ isAvoided(h) ? '✓ ' : '' }}{{ h }}</button>
                }
              </div>
              <form class="row" (submit)="$event.preventDefault(); addCustom()">
                <input class="field" placeholder="משהו אחר? למשל: מאג" [value]="custom()" (input)="custom.set($any($event.target).value)" maxlength="40" />
                <button type="submit" class="btn" [disabled]="!custom().trim()">הוספה</button>
              </form>
              @if (customAvoid().length) {
                <div class="chips">
                  @for (a of customAvoid(); track a) {
                    <span class="chip on chip-remove">{{ a }}<button type="button" class="x light" (click)="brief.removeAvoid(a)" [attr.aria-label]="'הסרת ' + a"><app-icon name="close" [size]="14" /></button></span>
                  }
                </div>
              }
            </div>
          }
        </section>

        <section class="card fold">
          <button type="button" class="fold-head" (click)="notesOpen.set(!notesOpen())" [attr.aria-expanded]="notesOpen()">
            <span class="ico warm"><app-icon name="edit" [size]="20" /></span>
            <span class="txt"><b>עוד משהו במילים שלך</b><span class="small muted">לא חובה · אופי, חלומות, בדיחות פנימיות</span></span>
            <app-icon name="chevron" class="rot" [class.open]="notesOpen()" />
          </button>
          @if (notesOpen()) {
            <div class="fold-body">
              <textarea class="field" rows="3" maxlength="400" placeholder="למשל: תמיד אומרת שהיא רוצה ללמוד לצלם בלילה"
                        [value]="d().notes" (input)="brief.patch({ notes: $any($event.target).value })"></textarea>
              <p class="small faint">לא לכתוב שמות מלאים, טלפונים או כתובות. הטקסט משמש רק כדי לדייק רעיונות.</p>
            </div>
          }
        </section>

        <button type="button" class="btn btn-primary btn-block" [disabled]="!d().ageRange" (click)="next()">המשך</button>
      </main>
    </ion-content>
  `,
  styles: `
    .back { --color: var(--ink); --icon-font-size: 22px; }
    .x { border: 0; background: transparent; color: inherit; display: inline-flex; padding: 4px; }
    .pick { background: var(--surface); box-shadow: inset 0 0 0 2px var(--line); color: var(--accent); }
    .suggest { align-items: center; }
    .fold { overflow: hidden; }
    .fold-head { width: 100%; display: flex; align-items: center; gap: 14px; padding: 16px 18px; border: 0; background: transparent; color: var(--ink); text-align: start; }
    .fold-head .ico { flex-shrink: 0; width: 40px; height: 40px; border-radius: 12px; background: var(--accent-soft); color: var(--accent); display: flex; align-items: center; justify-content: center; }
    .fold-head .ico.warm { background: #F3ECE3; color: #6E5440; }
    .fold-head .txt { flex-grow: 1; display: flex; flex-direction: column; gap: 2px; font-size: 16px; }
    .rot { color: var(--faint); transform: rotate(-90deg); transition: transform .2s; }
    .rot.open { transform: rotate(90deg); }
    .fold-body { padding: 0 18px 18px; display: flex; flex-direction: column; gap: 12px; }
    .fold-body .row .field { flex-grow: 1; }
  `
})
export class AboutPage {
  protected readonly brief = inject(BriefStore);
  private readonly pickers = inject(Pickers);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  protected readonly d = this.brief.draft;
  protected readonly p = computed(() => pronouns(this.d().relationship?.key));
  protected readonly alreadyHas = ALREADY_HAS;
  protected readonly dislikes = DISLIKES;
  protected readonly avoidOpen = signal(this.d().avoid.length > 0);
  protected readonly notesOpen = signal(!!this.d().notes);
  protected readonly custom = signal('');
  protected readonly recommended = signal<string[]>([]);
  protected readonly total = signal(0);

  protected readonly suggestions = computed(() =>
    this.recommended().filter(r => !this.d().interests.includes(r)).slice(0, 5));
  protected readonly customAvoid = computed(() =>
    this.d().avoid.filter(a => !ALREADY_HAS.includes(a) && !DISLIKES.includes(a)));

  constructor() { void this.loadSuggestions(); }

  isAvoided(t: string) { return this.d().avoid.includes(t); }
  toggleAvoid(t: string) { if (this.isAvoided(t)) this.brief.removeAvoid(t); else this.brief.addAvoid(t); }

  addCustom() {
    this.brief.addAvoid(this.custom());
    this.custom.set('');
  }

  async pickAge() {
    const o = await this.pickers.ageRange(this.d().ageRange?.key ?? null);
    if (o) {
      this.brief.patch({ ageRange: o });
      void this.loadSuggestions();
    }
  }

  async pickInterests() {
    const result = await this.pickers.interests(this.d().relationship?.key ?? null, this.d().ageRange?.key ?? null, this.d().interests);
    if (result) this.brief.patch({ interests: result });
  }

  next() { void this.router.navigateByUrl('/new/budget'); }

  private async loadSuggestions() {
    try {
      const data = await firstValueFrom(this.api.interests(this.d().relationship?.key ?? null, this.d().ageRange?.key ?? null));
      this.recommended.set(data.recommended);
      this.total.set(data.total);
    } catch { /* suggestions are optional */ }
  }
}
