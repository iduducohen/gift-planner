import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonBackButton, IonContent, NavController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BriefStore } from '../../core/brief.store';
import { avatar, inDays, pronouns, shortDate, tint, toIso } from '../../core/format';
import { Dialogs } from '../../core/dialogs';
import { IdeaActions } from '../../core/idea-actions.service';
import { RecipientDto, SavedIdeaDto } from '../../core/models';
import { Pickers } from '../../core/pickers';
import { Icon } from '../../ui/icon';

const REACTIONS: { key: string; label: string }[] = [
  { key: 'loved', label: 'אהבה מאוד' }, { key: 'liked', label: 'אהבה' }, { key: 'meh', label: 'ככה ככה' }, { key: 'dontrepeat', label: 'לא לחזור על זה' }
];

@Component({
  selector: 'app-person',
  imports: [IonContent, IonBackButton, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow">
        <div class="between">
          <ion-back-button defaultHref="/people" text="" class="icon-btn" aria-label="חזרה" />
          @if (r()) { <button type="button" class="btn edit" (click)="rename()">עריכה</button> }
        </div>

        @if (r(); as r) {
          <div class="row">
            <span class="av" [style.background]="av(r.nickname)[0]" [style.color]="av(r.nickname)[1]">{{ r.nickname.charAt(0) }}</span>
            <div class="stack-sm tight"><h1>{{ r.nickname }}</h1><span class="muted">{{ r.relationshipLabel }} · {{ r.ageRangeKey }}</span></div>
          </div>

          @if (r.next; as n) {
            <div class="next card">
              <div class="stack-sm tight"><span class="small accent">{{ inDays(n.daysLeft) }}</span><b>{{ n.label }} · {{ date(n.date) }}</b></div>
              <button type="button" class="btn btn-primary" (click)="newIdeas()">רעיונות חדשים</button>
            </div>
          } @else {
            <button type="button" class="btn btn-primary btn-block" (click)="newIdeas()">רעיונות חדשים {{ p().forHer }}</button>
          }

          <section class="stack">
            <div class="between"><h2>מה {{ p().her }} {{ p().likes }}</h2><button type="button" class="link" (click)="editInterests()">עריכה</button></div>
            <div class="chips">
              @for (i of r.interests; track i) { <span class="chip chip-accent">{{ i }}</span> }
              @empty { <span class="muted">עוד לא בחרת. <button type="button" class="link" (click)="editInterests()">בחירה מהרשימה</button></span> }
            </div>
            @if (r.avoid.length) {
              <div class="chips"><span class="small muted">לא להציע:</span>@for (a of r.avoid; track a) { <span class="chip">{{ a }}</span> }</div>
            }
          </section>

          <section class="stack">
            <div class="between"><h2>תאריכים</h2><button type="button" class="link" (click)="addOccasion()">+ הוספה</button></div>
            <div class="card list">
              @for (o of r.occasions; track o.id) {
                <div class="line">
                  <span class="stack-sm tight"><b>{{ o.label }}</b><span class="small muted">{{ o.day }}.{{ o.month }} · תזכורת {{ o.remindDaysBefore }} ימים לפני</span></span>
                  <button type="button" class="icon-btn ghost" (click)="removeOccasion(o.id)" [attr.aria-label]="'מחיקת ' + o.label"><app-icon name="trash" [size]="18" /></button>
                </div>
              } @empty { <div class="line muted">אין תאריכים. הוסיפו יום הולדת ונזכיר לכם בזמן.</div> }
            </div>
          </section>

          <section class="stack">
            <div class="between"><h2>מתנות קודמות</h2><button type="button" class="link" (click)="addGift()">+ הוספה</button></div>
            <div class="card list">
              @for (g of r.history; track g.id) {
                <div class="line"><span class="stack-sm tight"><b>{{ g.title }}</b><span class="small muted">{{ date(g.givenOn) }}</span></span>
                  @if (g.reaction) { <span class="chip">{{ reactionLabel(g.reaction) }}</span> }</div>
              } @empty { <div class="line muted">כאן יופיעו מתנות שנתת, כדי שלא נציע אותן שוב.</div> }
            </div>
          </section>

          @if (saved().length) {
            <section class="stack">
              <div class="between"><h2>שמורים {{ p().forHer }}</h2><a routerLink="/saved" class="link">הכול</a></div>
              <div class="hscroll">
                @for (s of saved(); track s.idea.id) {
                  <a class="mini" [routerLink]="['/idea', s.idea.id]">
                    <span class="thumb" [style.background]="t(s)[0]" [style.color]="t(s)[1]"><app-icon [name]="s.idea.iconKey" [size]="26" [stroke]="1.4" /></span>
                    <span class="small">{{ s.idea.title }}</span>
                  </a>
                }
              </div>
            </section>
          }

          <button type="button" class="btn btn-ghost danger" (click)="remove()">מחיקת {{ r.nickname }} מהרשימה</button>
        } @else {
          <div class="skel" style="height: 60px; width: 60%"></div><div class="skel" style="height: 120px"></div>
        }
      </main>
    </ion-content>
  `,
  styles: `
    .edit { min-height: 40px; border-radius: 999px; font-size: 15px; }
    .av { width: 64px; height: 64px; border-radius: 999px; font-family: var(--font-head); font-size: 24px; font-weight: 600; display: flex; align-items: center; justify-content: center; }
    .tight { gap: 2px; }
    .accent { color: var(--accent); font-weight: 700; }
    .next { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 16px 18px; background: linear-gradient(135deg, #fff, var(--accent-soft)); }
    .next .btn { min-height: 46px; font-size: 15px; border-radius: 14px; }
    .link { border: 0; background: none; padding: 0; color: var(--accent); font-weight: 700; font-size: 15px; text-decoration: none; }
    .list { overflow: hidden; }
    .line { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 14px 16px; }
    .line + .line { border-top: 1px solid var(--line); }
    .ghost { box-shadow: none; background: transparent; color: var(--faint); }
    .mini { flex-shrink: 0; width: 132px; display: flex; flex-direction: column; gap: 8px; color: var(--ink); text-decoration: none; font-weight: 700; }
    .thumb { height: 96px; border-radius: 16px; display: flex; align-items: center; justify-content: center; }
    .danger { color: var(--accent); }
  `
})
export class PersonPage {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly nav = inject(NavController);
  private readonly pickers = inject(Pickers);
  private readonly dialogs = inject(Dialogs);
  private readonly brief = inject(BriefStore);
  private readonly actions = inject(IdeaActions);

  readonly id = input.required<string>();
  protected readonly r = signal<RecipientDto | null>(null);
  protected readonly saved = signal<SavedIdeaDto[]>([]);
  protected readonly p = computed(() => pronouns(this.r()?.relationshipKey));
  protected readonly av = avatar;
  protected readonly date = shortDate;
  protected readonly inDays = inDays;
  protected t = (s: SavedIdeaDto) => tint(s.idea.kind);
  protected reactionLabel = (k: string) => REACTIONS.find(x => x.key === k.toLowerCase())?.label ?? k;

  constructor() {
    effect(() => {
      const id = this.id();
      this.api.recipient(id).subscribe({ next: r => this.r.set(r), error: () => void this.actions.toast('לא נמצא') });
      this.api.saved(id).subscribe({ next: s => this.saved.set(s), error: () => this.saved.set([]) });
    });
  }

  private upsert(patch: Partial<RecipientDto>) {
    const r = { ...this.r()!, ...patch };
    return firstValueFrom(this.api.updateRecipient(r.id, {
      nickname: r.nickname, relationshipKey: r.relationshipKey, ageRangeKey: r.ageRangeKey, interests: r.interests, avoid: r.avoid, notes: r.notes
    }));
  }

  async rename() {
    const name = await this.dialogs.askText('שינוי כינוי', 'כינוי בלבד, בלי שם מלא.', this.r()!.nickname);
    if (name) this.r.set(await this.upsert({ nickname: name }));
  }

  async editInterests() {
    const r = this.r()!;
    const result = await this.pickers.interests(r.relationshipKey, r.ageRangeKey, r.interests);
    if (result) this.r.set(await this.upsert({ interests: result }));
  }

  async addOccasion() {
    const type = await this.pickers.occasion(null);
    if (!type) return;
    const when = await this.dialogs.askText(`מתי ${type.label}?`, 'יום.חודש, למשל 14.3', '', '14.3');
    const m = when?.match(/^(\d{1,2})[./](\d{1,2})$/);
    if (!m) { if (when) await this.actions.toast('פורמט: יום.חודש'); return; }
    try { this.r.set(await firstValueFrom(this.api.addOccasion(this.r()!.id, type.key, +m[2], +m[1]))); }
    catch { await this.actions.toast('תאריך לא תקין'); }
  }

  async removeOccasion(occasionId: string) {
    this.r.set(await firstValueFrom(this.api.removeOccasion(this.r()!.id, occasionId)));
  }

  async addGift() {
    const title = await this.dialogs.askText('איזו מתנה נתת?', 'נזכור לא להציע אותה שוב.');
    if (!title) return;
    this.r.set(await firstValueFrom(this.api.recordGift(this.r()!.id, title, toIso(new Date()), null)));
  }

  async newIdeas() {
    const r = this.r()!;
    const ages = await firstValueFrom(this.api.ageRanges$);
    this.brief.fromRecipient(r, ages.flatMap(g => g.options).find(o => o.key === r.ageRangeKey)?.label ?? null);
    await this.router.navigateByUrl(this.brief.draft().occasion ? '/new/budget' : '/');
  }

  async remove() {
    const r = this.r()!;
    if (!(await this.dialogs.confirm(`למחוק את ${r.nickname}?`, 'כל התאריכים וההיסטוריה יימחקו.', 'מחיקה', true))) return;
    await firstValueFrom(this.api.deleteRecipient(r.id));
    this.nav.navigateBack('/people');
  }
}
