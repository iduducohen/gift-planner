import { ChangeDetectionStrategy, Component, computed, effect, inject, input, output, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ModalController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../core/api.service';
import { BriefStore } from '../core/brief.store';
import { priceLabel, pronouns, shekel, tint } from '../core/format';
import { IdeaActions } from '../core/idea-actions.service';
import { IdeaDetailDto } from '../core/models';
import { BuySheet } from './buy-sheet';
import { Icon } from './icon';

/** Idea detail — rendered as a page on mobile and inside a side drawer on desktop. */
@Component({
  selector: 'app-idea-detail',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (detail(); as dd) {
      <div class="hero" [style.background]="colors()[0]" [style.color]="colors()[1]">
        <app-icon [name]="dd.idea.iconKey" [size]="64" [stroke]="1.2" />
        <div class="bar">
          <button type="button" class="icon-btn" (click)="closed.emit()" [attr.aria-label]="inDrawer() ? 'סגירה' : 'חזרה'">
            <app-icon [name]="inDrawer() ? 'close' : 'back'" />
          </button>
          <div class="row">
            <button type="button" class="icon-btn" (click)="share()" aria-label="שיתוף"><app-icon name="external" /></button>
            <button type="button" class="icon-btn" [class.saved]="dd.idea.saved" (click)="toggleSave()"
                    [attr.aria-label]="dd.idea.saved ? 'הסרה מהשמורים' : 'שמירה'" [attr.aria-pressed]="dd.idea.saved">
              <app-icon name="heart" />
            </button>
          </div>
        </div>
      </div>

      <div class="content">
        <div class="stack-sm">
          <div class="row wrap">
            <span class="kind">{{ dd.idea.kindLabel }}</span>
            @if (dd.offer && dd.offer.discountPercent > 0) { <span class="deal-badge">‎-{{ dd.offer.discountPercent }}% רק דרכנו</span> }
          </div>
          <h1>{{ dd.idea.title }}</h1>
          <div class="price">
            <b>{{ price() }}</b>
            @if (dd.offer && dd.offer.discountPercent > 0) {
              <s>{{ money(dd.offer.listPrice) }}</s>
              <span class="save-amt">חוסכים {{ money(dd.offer.listPrice - dd.offer.finalPrice) }}</span>
            }
          </div>
          @if (!dd.offer) { <span class="small faint">מחיר משוער לפי הקטלוג · אומת {{ dd.catalogVerifiedAt }}</span> }
        </div>

        @if (dd.offer; as o) {
          <div class="partner card">
            @if (o.partnerImage) {
              <img [src]="o.partnerImage" [alt]="o.partnerName" class="partner-img">
            } @else {
              <span class="logo">{{ o.partnerName.charAt(0) }}</span>
            }
            <span class="txt">
              <b>{{ o.partnerName }}</b>
              <span class="small" [class.ok]="o.partnerVerified">{{ o.partnerVerified ? '✓ עסק שותף מאומת' : 'עסק' }} · {{ o.partnerRegion }}</span>
              @if (o.partnerPhone) { <span class="phone"><app-icon name="phone" [size]="14" />{{ o.partnerPhone }}</span> }
            </span>
          </div>
        }

        <section class="stack-sm">
          <h2>למה זה מתאים {{ p().toHer }}</h2>
          <p class="body">{{ dd.idea.why }}</p>
          @if (dd.idea.emotionalValue) { <p class="body muted">{{ dd.idea.emotionalValue }}</p> }
        </section>

        @if (dd.includes.length) {
          <section class="stack-sm">
            <h2>מה כולל</h2>
            <ul class="includes">@for (i of dd.includes; track i) { <li><app-icon name="check" [size]="16" />{{ i }}</li> }</ul>
          </section>
        }

        @if (dd.idea.howTo) {
          <section class="stack-sm"><h2>איך להפוך את זה לרגע</h2><p class="body">{{ dd.idea.howTo }}</p></section>
        }

        @if (dd.idea.upgrade) {
          <div class="upgrade"><app-icon name="sparkle" /><span><b>שדרוג:</b> {{ dd.idea.upgrade }}</span></div>
        }

        <button type="button" class="btn btn-block plan-btn" [disabled]="busy()" (click)="makePlan()">
          <app-icon name="calendar" [size]="18" /> הפוך את זה לתוכנית יום שלמה
        </button>
        @if (dd.idea.isAiWritten) { <p class="small faint">הניסוח נכתב בעזרת AI. המחירים והפרטים מגיעים מהעסקים.</p> }
      </div>

      <div class="buybar">
        <span class="stack-sm tight"><b class="big">{{ price() }}</b>
          <span class="small muted">{{ dd.offer?.discountPercent ? 'כולל ההנחה שלך' : 'מחיר משוער' }}</span></span>
        <button type="button" class="btn btn-primary" (click)="buy()">{{ dd.offer ? 'לרכישה' : 'לתכנון' }}</button>
      </div>
    } @else {
      <div class="loading">
        <div class="skel" style="height: 240px; border-radius: 0"></div>
        <div class="content"><div class="skel" style="height: 28px; width: 70%"></div><div class="skel" style="height: 16px; width: 40%"></div>
          <div class="skel" style="height: 90px"></div></div>
      </div>
    }
  `,
  styles: `
    :host { display: block; position: relative; padding-bottom: 96px; }
    .hero { position: relative; height: 260px; display: flex; align-items: center; justify-content: center; }
    .bar { position: absolute; inset: 16px var(--gutter) auto; display: flex; justify-content: space-between; }
    .icon-btn.saved { color: var(--accent); }
    .content { padding: 22px var(--gutter) 24px; display: flex; flex-direction: column; gap: 22px; }
    .wrap { flex-wrap: wrap; gap: 8px; }
    .kind { font-size: 14px; font-weight: 700; color: var(--accent); }
    h1 { font-size: 28px; }
    .price { display: flex; align-items: baseline; gap: 10px; flex-wrap: wrap; }
    .price b { font-size: 26px; }
    .price s { color: var(--faint); font-size: 16px; }
    .save-amt { color: var(--deal); font-weight: 700; font-size: 15px; }
    .partner { display: flex; align-items: center; gap: 14px; padding: 14px 16px; }
    .logo { width: 44px; height: 44px; border-radius: 12px; background: var(--surface-2); display: flex; align-items: center; justify-content: center; font-weight: 700; flex-shrink: 0; }
    .partner-img { width: 44px; height: 44px; border-radius: 12px; object-fit: cover; flex-shrink: 0; }
    .partner .txt { display: flex; flex-direction: column; gap: 4px; }
    .phone { display: flex; align-items: center; gap: 5px; font-size: 13px; color: var(--muted); font-weight: 500; }
    .phone app-icon { color: var(--accent); }
    .ok { color: var(--deal); font-weight: 600; }
    .body { font-size: 17px; line-height: 1.6; }
    .includes { list-style: none; margin: 0; padding: 0; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    .includes li { display: flex; align-items: center; gap: 8px; padding: 12px; border-radius: 14px; background: var(--surface); font-weight: 600; font-size: 15px; }
    .includes app-icon { color: var(--deal); }
    .upgrade { display: flex; gap: 12px; align-items: flex-start; padding: 16px; border-radius: 16px; background: var(--accent-soft); color: var(--ink-2); line-height: 1.5; }
    .upgrade app-icon { color: var(--accent); }
    .plan-btn { background: var(--surface); box-shadow: var(--shadow-sm); }
    .buybar { position: fixed; inset-inline: 0; bottom: 0; z-index: 5; display: flex; align-items: center; justify-content: space-between; gap: 16px;
      padding: 14px var(--gutter) calc(14px + env(safe-area-inset-bottom)); background: rgba(251, 248, 244, .97); border-top: 1px solid var(--line); }
    :host(.in-drawer) .buybar { position: sticky; }
    .buybar .btn { min-width: 150px; }
    .big { font-size: 20px; }
    .tight { gap: 0; }
    .loading .content { gap: 14px; }
  `,
  host: { '[class.in-drawer]': 'inDrawer()' }
})
export class IdeaDetail {
  private readonly api = inject(ApiService);
  private readonly actions = inject(IdeaActions);
  private readonly modals = inject(ModalController);
  private readonly router = inject(Router);
  private readonly brief = inject(BriefStore);

  readonly ideaId = input.required<string>();
  readonly inDrawer = input(false);
  readonly closed = output<void>();
  readonly savedChange = output<{ id: string; saved: boolean }>();

  protected readonly detail = signal<IdeaDetailDto | null>(null);
  protected readonly busy = signal(false);
  protected readonly colors = computed(() => tint(this.detail()?.idea.kind ?? 'experience'));
  protected readonly price = computed(() => {
    const d = this.detail();
    if (!d) return '';
    return d.offer ? shekel(d.offer.finalPrice) : priceLabel(d.idea);
  });
  protected readonly p = computed(() => pronouns(this.brief.draft().relationship?.key));
  protected readonly money = shekel;

  constructor() {
    effect(() => {
      const id = this.ideaId();
      this.detail.set(null);
      this.api.idea(id).subscribe({ next: d => this.detail.set(d), error: () => void this.actions.toast('הרעיון לא נמצא') });
    });
  }

  async toggleSave() {
    const d = this.detail();
    if (!d) return;
    const saved = await this.actions.toggleSave(d.idea);
    this.detail.set({ ...d, idea: { ...d.idea, saved } });
    this.savedChange.emit({ id: d.idea.id, saved });
  }

  async share() {
    const d = this.detail();
    if (!d) return;
    const text = `${d.idea.title} – ${d.idea.why}`;
    try {
      if (navigator.share) await navigator.share({ title: d.idea.title, text });
      else {
        await navigator.clipboard.writeText(text);
        await this.actions.toast('הועתק ללוח');
      }
    } catch { /* user cancelled */ }
  }

  async makePlan() {
    const d = this.detail();
    if (!d) return;
    this.busy.set(true);
    try {
      const plan = await firstValueFrom(this.api.createPlan(d.idea.id, this.brief.draft().eventDate));
      if (this.inDrawer()) await this.modals.dismiss();
      await this.router.navigate(['/plans', plan.id]);
    } finally {
      this.busy.set(false);
    }
  }

  async buy() {
    const d = this.detail();
    if (!d) return;
    if (!d.offer) return this.makePlan();
    const m = await this.modals.create({
      component: BuySheet,
      componentProps: { detail: d, eventDate: this.brief.draft().eventDate, occasionLabel: this.brief.draft().occasion?.label ?? null },
      cssClass: 'sheet'
    });
    await m.present();
    const { data } = await m.onWillDismiss<{ planId: string }>();
    if (data?.planId) {
      if (this.inDrawer()) await this.modals.dismiss();
      await this.router.navigate(['/plans', data.planId]);
    }
  }
}
