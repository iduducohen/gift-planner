import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonContent, IonRefresher, IonRefresherContent, ViewWillEnter } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BriefStore } from '../../core/brief.store';
import { avatar, inDays } from '../../core/format';
import { IdeaActions } from '../../core/idea-actions.service';
import { RecipientSummaryDto } from '../../core/models';
import { Dialogs } from '../../core/dialogs';
import { Pickers } from '../../core/pickers';
import { Icon } from '../../ui/icon';

@Component({
  selector: 'app-people',
  imports: [IonContent, IonRefresher, IonRefresherContent, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <ion-refresher slot="fixed" (ionRefresh)="refresh($any($event))"><ion-refresher-content /></ion-refresher>
      <main class="page">
        <div class="between">
          <h1>האנשים שלי</h1>
          <button type="button" class="icon-btn add" (click)="add()" aria-label="הוספת אדם"><app-icon name="plus" /></button>
        </div>

        @if (next(); as n) {
          <a class="soon card" [routerLink]="['/people', n.id]">
            <span class="av big" [style.background]="av(n.nickname)[0]" [style.color]="av(n.nickname)[1]">{{ n.nickname.charAt(0) }}</span>
            <span class="stack-sm tight grow">
              <span class="small accent">הבא בתור</span>
              <b class="name">{{ n.next!.label }} של {{ n.nickname }}</b>
              <span class="muted">{{ when(n.next!.daysLeft) }}</span>
            </span>
            <button type="button" class="btn btn-primary go" (click)="$event.preventDefault(); $event.stopPropagation(); ideasFor(n)">רעיונות</button>
          </a>
        }

        @if (loaded() && !people().length) {
          <div class="empty card">
            <app-icon name="people" [size]="40" [stroke]="1.4" />
            <b>עוד אין כאן אף אחד</b>
            <p class="muted">שמרו אדם אחרי חיפוש מתנה, או הוסיפו עכשיו. נזכיר לכם לפני כל תאריך חשוב.</p>
            <button type="button" class="btn btn-primary" (click)="add()">הוספת אדם</button>
          </div>
        }

        <div class="grid">
          @for (p of people(); track p.id) {
            <a class="person card" [routerLink]="['/people', p.id]">
              <span class="av" [style.background]="av(p.nickname)[0]" [style.color]="av(p.nickname)[1]">{{ p.nickname.charAt(0) }}</span>
              <span class="stack-sm tight grow">
                <b>{{ p.nickname }}</b>
                <span class="small muted">{{ p.next ? p.next.label + ' · ' + p.next.date.slice(8) + '.' + (+p.next.date.slice(5, 7)) : p.relationshipLabel }}</span>
              </span>
              @if (p.next) { <span class="when" [class.hot]="p.next.daysLeft <= 30">{{ when(p.next.daysLeft) }}</span> }
            </a>
          } @empty {
            @if (!loaded()) { @for (s of [1, 2, 3]; track s) { <div class="skel" style="height: 76px; border-radius: 20px"></div> } }
          }
        </div>
      </main>
    </ion-content>
  `,
  styles: `
    .add { background: var(--accent); color: #fff; }
    .soon { display: flex; align-items: center; gap: 14px; padding: 18px; text-decoration: none; color: var(--ink); background: linear-gradient(135deg, #fff, var(--accent-soft)); }
    .grow { flex-grow: 1; min-width: 0; }
    .tight { gap: 2px; }
    .accent { color: var(--accent); font-weight: 700; }
    .name { font-family: var(--font-head); font-size: 20px; font-weight: 600; }
    .go { min-height: 44px; font-size: 15px; border-radius: 14px; padding: 0 16px; }
    .grid { display: grid; gap: 12px; }
    @media (min-width: 900px) { .grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
    .person { display: flex; align-items: center; gap: 14px; padding: 14px 16px; text-decoration: none; color: var(--ink); }
    .av { flex-shrink: 0; width: 48px; height: 48px; border-radius: 999px; font-family: var(--font-head); font-size: 18px; font-weight: 600; display: flex; align-items: center; justify-content: center; }
    .av.big { width: 60px; height: 60px; font-size: 22px; }
    .when { flex-shrink: 0; min-height: 30px; padding: 0 12px; border-radius: 999px; background: var(--surface-2); color: var(--muted); font-size: 13px; font-weight: 700; display: flex; align-items: center; }
    .when.hot { background: var(--accent-soft); color: var(--accent); }
    .empty { display: flex; flex-direction: column; align-items: center; gap: 10px; padding: 32px 24px; text-align: center; color: var(--muted); }
    .empty b { color: var(--ink); font-size: 18px; }
  `
})
export class PeoplePage implements ViewWillEnter {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly pickers = inject(Pickers);
  private readonly brief = inject(BriefStore);
  private readonly actions = inject(IdeaActions);
  private readonly dialogs = inject(Dialogs);

  protected readonly people = signal<RecipientSummaryDto[]>([]);
  protected readonly loaded = signal(false);
  protected readonly next = computed(() => this.people().find(p => p.next && p.next.daysLeft <= 60) ?? null);
  protected readonly av = avatar;
  protected readonly when = inDays;

  ionViewWillEnter() { void this.load(); }

  async refresh(ev: CustomEvent) {
    await this.load();
    (ev.target as HTMLIonRefresherElement).complete();
  }

  private async load() {
    try { this.people.set(await firstValueFrom(this.api.recipients())); }
    catch { await this.actions.toast('לא הצלחנו לטעון'); }
    finally { this.loaded.set(true); }
  }

  async add() {
    const rel = await this.pickers.relationship(null);
    if (!rel) return;
    const age = await this.pickers.ageRange(null);
    if (!age) return;
    const nickname = await this.dialogs.askText('איך לקרוא לו/ה?', 'כינוי בלבד, למשל: אמא, דני. בלי שם מלא.', rel.label.split(' / ')[0]);
    if (!nickname) return;
    const r = await firstValueFrom(this.api.createRecipient({ nickname, relationshipKey: rel.key, ageRangeKey: age.key, interests: [], avoid: [], notes: null }));
    await this.router.navigate(['/people', r.id]);
  }

  async ideasFor(p: RecipientSummaryDto) {
    const [full, ages] = await Promise.all([firstValueFrom(this.api.recipient(p.id)), firstValueFrom(this.api.ageRanges$)]);
    this.brief.fromRecipient(full, ages.flatMap(g => g.options).find(o => o.key === full.ageRangeKey)?.label ?? null);
    await this.router.navigateByUrl('/new/budget');
  }
}
