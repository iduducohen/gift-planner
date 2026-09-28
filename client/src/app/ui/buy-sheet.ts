import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { Browser } from '@capacitor/browser';
import { ModalController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../core/api.service';
import { CelebrateService } from '../core/celebrate.service';
import { parseDate, shekel, shortDate, tint, toIso } from '../core/format';
import { IdeaDetailDto, PurchaseChannel, PurchaseResultDto } from '../core/models';
import { Icon } from './icon';

/**
 * Buy flow. Business model: the partner gives our users a discount (coupon + tracked link),
 * we send them qualified demand. When the gift is chosen — confetti and balloons.
 */
@Component({
  selector: 'app-buy-sheet',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'ion-page' },
  template: `
    <div class="sheet-scroll">
      @if (!result()) {
        <div class="sheet-body">
          <div class="row">
            <button type="button" class="icon-btn close" (click)="dismiss()" aria-label="סגירה"><app-icon name="close" /></button>
            <span class="thumb" [style.background]="colors()[0]" [style.color]="colors()[1]"><app-icon [name]="detail().idea.iconKey" [size]="28" [stroke]="1.4" /></span>
            <span class="stack-sm tight"><b class="title">{{ detail().idea.title }}</b><span class="muted">{{ detail().offer?.partnerName }}</span></span>
          </div>

          <div class="stack-sm">
            <b>לאיזה תאריך?</b>
            <div class="chips">
              @for (o of dateOptions(); track o.iso) {
                <button type="button" class="chip" [attr.aria-pressed]="date() === o.iso" (click)="setDate(o.iso)">{{ o.label }}</button>
              }
              <label class="chip other" [attr.aria-pressed]="isOther()">
                תאריך אחר
                <input type="date" [min]="today" (change)="setDate($any($event.target).value || null)" aria-label="תאריך אחר" />
              </label>
            </div>
          </div>

          <div class="stack-sm" role="radiogroup" aria-label="איך תרצה לקנות?">
            <b>איך תרצה לקנות?</b>
            @for (w of ways; track w.key) {
              <button type="button" role="radio" class="way" [attr.aria-checked]="channel() === w.key" (click)="channel.set(w.key)">
                <span class="dot"></span>
                <span class="stack-sm tight"><b>{{ w.title }}</b><span class="small muted">{{ w.sub }}</span></span>
              </button>
            }
          </div>

          <button type="button" class="btn btn-primary btn-block" [disabled]="busy()" (click)="confirm()">
            {{ channel() === 'partner' ? 'להזמנה באתר העסק' : 'לתשלום' }} · {{ price() }}
          </button>
          <p class="small muted center">התשלום והאספקה באחריות העסק. ההזמנה נשמרת אצלך בתוכנית.</p>
        </div>
      } @else {
        <div class="done" role="dialog" aria-modal="true" aria-label="ההפתעה בדרך">
          <div class="done-card">
            <span class="gift"><app-icon name="gift" [size]="34" [stroke]="1.6" /></span>
            <h1>ההפתעה בדרך!</h1>
            <p class="lead">{{ detail().idea.title }}{{ date() ? ' ל' + fmt(date()!) : '' }} נשמר בתוכנית. נזכיר לך כל משימה בזמן.</p>
            <div class="coupon">
              <span class="stack-sm tight"><span class="small">קוד ההנחה שלך</span><b class="code">{{ result()!.couponCode }}</b></span>
              <button type="button" class="btn copy" (click)="copy()">{{ copied() ? 'הועתק ✓' : 'העתק' }}</button>
            </div>
            @if (result()!.redirectUrl) {
              <button type="button" class="btn btn-dark btn-block" (click)="openPartner()">לאתר העסק <app-icon name="external" [size]="16" /></button>
            }
            <button type="button" class="btn btn-primary btn-block" (click)="toPlan()">לתוכנית של {{ occasionLabel() ?? 'האירוע' }}</button>
            <button type="button" class="btn btn-ghost" (click)="celebrate.celebrate()">לראות שוב</button>
          </div>
        </div>
      }
    </div>
  `,
  styles: `
    .sheet-body { padding: 24px var(--gutter) 32px; display: flex; flex-direction: column; gap: 22px; }
    .close { order: 3; margin-inline-start: auto; }
    .thumb { flex-shrink: 0; width: 60px; height: 60px; border-radius: 16px; display: flex; align-items: center; justify-content: center; }
    .title { font-family: var(--font-head); font-size: 20px; font-weight: 600; }
    .tight { gap: 2px; }
    .other { position: relative; overflow: hidden; }
    .other input { position: absolute; inset: 0; opacity: 0; }
    .way { display: flex; align-items: center; gap: 14px; padding: 16px; border-radius: 18px; background: var(--surface); border: 2px solid var(--surface); color: var(--ink); text-align: start; }
    .way[aria-checked='true'] { border-color: var(--accent); }
    .dot { flex-shrink: 0; width: 22px; height: 22px; border-radius: 999px; border: 2px solid #CFC2B4; box-shadow: inset 0 0 0 4px #fff; }
    .way[aria-checked='true'] .dot { border-color: var(--accent); background: var(--accent); }
    .center { text-align: center; }
    .done { min-height: 100%; display: flex; align-items: center; justify-content: center; padding: 24px; }
    .done-card { width: 100%; max-width: 420px; display: flex; flex-direction: column; align-items: center; gap: 14px; padding: 32px 24px;
      border-radius: 28px; background: #fff; text-align: center; box-shadow: 0 24px 60px rgba(34, 27, 23, .16); animation: gp-pop .45s ease-out both; }
    .gift { width: 68px; height: 68px; border-radius: 999px; background: var(--accent-soft); color: var(--accent); display: flex; align-items: center; justify-content: center; }
    h1 { font-size: 28px; }
    .coupon { align-self: stretch; display: flex; align-items: center; justify-content: space-between; padding: 14px 16px; border-radius: 16px; background: var(--deal-soft); color: var(--deal); text-align: start; }
    .code { font-family: var(--font-head); font-size: 20px; letter-spacing: 1px; }
    .copy { min-height: 40px; border-radius: 999px; background: #fff; color: var(--deal); font-size: 14px; }
  `
})
export class BuySheet {
  private readonly api = inject(ApiService);
  private readonly modalCtrl = inject(ModalController);
  protected readonly celebrate = inject(CelebrateService);

  readonly detail = input.required<IdeaDetailDto>();
  readonly eventDate = input<string | null>(null);
  readonly occasionLabel = input<string | null>(null);

  protected readonly today = toIso(new Date());
  protected readonly ways: { key: PurchaseChannel; title: string; sub: string }[] = [
    { key: 'partner', title: 'באתר של העסק', sub: 'ההנחה מופעלת אוטומטית דרך הקישור שלנו' },
    { key: 'in_app', title: 'כאן באפליקציה', sub: 'העסק מאשר תוך שעה ומעדכן אותך' }
  ];
  protected readonly channel = signal<PurchaseChannel>('partner');
  private readonly picked = signal<string | null | undefined>(undefined);
  protected readonly date = computed(() => (this.picked() === undefined ? this.eventDate() : this.picked()) ?? null);
  protected readonly busy = signal(false);
  protected readonly result = signal<PurchaseResultDto | null>(null);
  protected readonly copied = signal(false);
  protected readonly colors = computed(() => tint(this.detail().idea.kind));
  protected readonly price = computed(() => shekel(this.detail().offer?.finalPrice ?? this.detail().idea.priceMin));
  protected readonly fmt = shortDate;

  /** The event date plus the day before (surprises often happen the evening before). */
  protected readonly dateOptions = computed(() => {
    const e = this.eventDate();
    if (!e) return [];
    const before = parseDate(e);
    before.setDate(before.getDate() - 1);
    const opts = [{ iso: e, label: shortDate(e) }];
    if (toIso(before) >= this.today) opts.push({ iso: toIso(before), label: shortDate(toIso(before)) });
    return opts;
  });
  protected readonly isOther = computed(() => !!this.date() && !this.dateOptions().some(o => o.iso === this.date()));

  setDate(v: string | null) { this.picked.set(v); }

  async confirm() {
    this.busy.set(true);
    try {
      const r = await firstValueFrom(this.api.purchase(this.detail().idea.id, this.date(), this.channel()));
      this.result.set(r);
      this.celebrate.celebrate();
    } finally {
      this.busy.set(false);
    }
  }

  async copy() {
    try {
      await navigator.clipboard.writeText(this.result()!.couponCode);
      this.copied.set(true);
    } catch { /* clipboard unavailable */ }
  }

  async openPartner() {
    const url = this.result()?.redirectUrl;
    if (url) await Browser.open({ url });
  }

  dismiss() { void this.modalCtrl.dismiss(null); }

  toPlan() { void this.modalCtrl.dismiss({ planId: this.result()!.planId }); }
}
