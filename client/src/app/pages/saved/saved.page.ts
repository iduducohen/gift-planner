import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonContent, ViewWillEnter } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { inDays, shortDate } from '../../core/format';
import { IdeaActions } from '../../core/idea-actions.service';
import { IdeaDto, PlanDto, SavedIdeaDto } from '../../core/models';
import { Icon } from '../../ui/icon';
import { IdeaCard } from '../../ui/idea-card';

@Component({
  selector: 'app-saved',
  imports: [IonContent, RouterLink, Icon, IdeaCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page">
        <h1>שמורים</h1>
        <div class="tabs" role="tablist">
          <button type="button" role="tab" [attr.aria-selected]="tab() === 'ideas'" (click)="tab.set('ideas')">רעיונות · {{ saved().length }}</button>
          <button type="button" role="tab" [attr.aria-selected]="tab() === 'plans'" (click)="tab.set('plans')">תוכניות · {{ plans().length }}</button>
        </div>

        @if (tab() === 'ideas') {
          @if (people().length > 1) {
            <div class="hscroll chips-row">
              <button type="button" class="chip" [attr.aria-pressed]="who() === null" (click)="who.set(null)">כולם</button>
              @for (p of people(); track p.id) {
                <button type="button" class="chip" [attr.aria-pressed]="who() === p.id" (click)="who.set(p.id)">{{ p.name }}</button>
              }
            </div>
          }
          @for (g of groups(); track g.key) {
            <section class="stack">
              <h2>{{ g.title }}</h2>
              <div class="grid">
                @for (s of g.items; track s.idea.id) {
                  <app-idea-card [idea]="s.idea" (open)="open($event)" (toggleSave)="unsave($event)" />
                }
              </div>
            </section>
          } @empty {
            @if (loaded()) {
              <div class="empty card"><app-icon name="heart" [size]="40" [stroke]="1.4" /><b>עוד לא שמרת רעיונות</b>
                <p class="muted">לחצו על הלב ברעיון שאהבתם, והוא יחכה כאן.</p><a routerLink="/" class="btn btn-primary">למתנה חדשה</a></div>
            }
          }
        } @else {
          <div class="stack">
            @for (p of plans(); track p.id) {
              <a class="plan card" [routerLink]="['/plans', p.id]">
                <span class="ico"><app-icon name="calendar" /></span>
                <span class="stack-sm tight grow"><b>{{ p.title }}</b><span class="small muted">{{ date(p.eventDate) }} · {{ doneOf(p) }}</span></span>
                <span class="when" [class.hot]="p.daysLeft <= 14">{{ when(p.daysLeft) }}</span>
              </a>
            } @empty {
              @if (loaded()) { <div class="empty card"><b>אין תוכניות עדיין</b><p class="muted">בחרו רעיון ולחצו "הפוך לתוכנית".</p></div> }
            }
          </div>
        }
      </main>
    </ion-content>
  `,
  styles: `
    .tabs { display: flex; gap: 4px; padding: 4px; border-radius: 16px; background: var(--surface-2); align-self: flex-start; }
    .tabs button { min-height: 42px; padding: 0 18px; border: 0; border-radius: 12px; background: transparent; font-size: 15px; font-weight: 700; color: var(--muted); }
    .tabs button[aria-selected='true'] { background: #fff; color: var(--ink); box-shadow: 0 1px 2px rgba(34, 27, 23, .06), 0 4px 12px rgba(34, 27, 23, .06); }
    .chips-row { gap: 8px; padding-bottom: 4px; }
    .grid { display: grid; gap: 14px; grid-template-columns: repeat(2, minmax(0, 1fr)); }
    @media (min-width: 700px) { .grid { grid-template-columns: repeat(3, minmax(0, 1fr)); } }
    @media (min-width: 1100px) { .grid { grid-template-columns: repeat(4, minmax(0, 1fr)); } }
    .plan { display: flex; align-items: center; gap: 14px; padding: 16px; text-decoration: none; color: var(--ink); }
    .ico { width: 44px; height: 44px; border-radius: 14px; background: var(--accent-soft); color: var(--accent); display: flex; align-items: center; justify-content: center; }
    .grow { flex-grow: 1; }
    .tight { gap: 2px; }
    .when { min-height: 30px; padding: 0 12px; border-radius: 999px; background: var(--surface-2); font-size: 13px; font-weight: 700; display: flex; align-items: center; color: var(--muted); }
    .when.hot { background: var(--accent-soft); color: var(--accent); }
    .empty { display: flex; flex-direction: column; align-items: center; gap: 10px; padding: 32px 24px; text-align: center; }
  `
})
export class SavedPage implements ViewWillEnter {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly actions = inject(IdeaActions);

  protected readonly tab = signal<'ideas' | 'plans'>('ideas');
  protected readonly saved = signal<SavedIdeaDto[]>([]);
  protected readonly plans = signal<PlanDto[]>([]);
  protected readonly loaded = signal(false);
  protected readonly who = signal<string | null>(null);
  protected readonly date = shortDate;
  protected readonly when = inDays;

  protected readonly people = computed(() => {
    const map = new Map<string, string>();
    for (const s of this.saved()) if (s.recipientId && s.recipientNickname) map.set(s.recipientId, s.recipientNickname);
    return [...map].map(([id, name]) => ({ id, name }));
  });

  /** Grouped by person ("בשביל אמא"), unassigned last. */
  protected readonly groups = computed(() => {
    const list = this.saved().filter(s => !this.who() || s.recipientId === this.who());
    const byKey = new Map<string, { key: string; title: string; items: SavedIdeaDto[] }>();
    for (const s of list) {
      const key = s.recipientId ?? '_';
      if (!byKey.has(key)) byKey.set(key, { key, title: s.recipientNickname ? `בשביל ${s.recipientNickname}` : 'בלי שיוך', items: [] });
      byKey.get(key)!.items.push(s);
    }
    return [...byKey.values()].sort((a, b) => (a.key === '_' ? 1 : b.key === '_' ? -1 : 0));
  });

  ionViewWillEnter() { void this.load(); }

  private async load() {
    try {
      const [s, p] = await Promise.all([firstValueFrom(this.api.saved()), firstValueFrom(this.api.plans())]);
      this.saved.set(s);
      this.plans.set(p.sort((a, b) => a.daysLeft - b.daysLeft));
    } catch {
      await this.actions.toast('לא הצלחנו לטעון');
    } finally {
      this.loaded.set(true);
    }
  }

  doneOf(p: PlanDto) { return `${p.tasks.filter(t => t.done).length}/${p.tasks.length} משימות`; }

  open(i: IdeaDto) { void this.router.navigate(['/idea', i.id]); }

  async unsave(i: IdeaDto) {
    const saved = await this.actions.toggleSave(i);
    if (!saved) this.saved.update(list => list.filter(s => s.idea.id !== i.id));
  }
}
