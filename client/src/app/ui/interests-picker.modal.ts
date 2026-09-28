import { ScrollingModule } from '@angular/cdk/scrolling';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { IonContent, IonFooter, IonHeader, IonToolbar, ModalController } from '@ionic/angular';
import { InterestsDto } from '../core/models';
import { Icon } from './icon';

type Row = { type: 'header'; title: string; count: number } | { type: 'item'; label: string };

/**
 * Multi-select of interests, tailored by relationship + age (the server returns only relevant categories).
 * The full list (150+ items) is rendered with CDK virtual scroll so only the visible rows exist in the DOM.
 */
@Component({
  selector: 'app-interests-picker',
  imports: [IonHeader, IonToolbar, IonContent, IonFooter, ScrollingModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-header class="ion-no-border">
      <ion-toolbar>
        <div class="head">
          <button class="icon-btn" type="button" (click)="close()" aria-label="סגירה"><app-icon name="close" /></button>
          <div>
            <h1>מה היא אוהבת?</h1>
            <p class="muted small">{{ data().total }} תחומים · מותאם ל{{ data().audienceLabel }}</p>
          </div>
        </div>
        <label class="search">
          <app-icon name="search" [size]="18" />
          <input type="search" placeholder="חיפוש תחום, למשל: צילום" [value]="q()" (input)="q.set($any($event.target).value)" />
        </label>
        @if (selected().length) {
          <div class="picked hscroll">
            @for (s of selected(); track s) {
              <button type="button" class="chip on chip-remove" (click)="toggle(s)" [attr.aria-label]="'הסרת ' + s">{{ s }} <app-icon name="close" [size]="14" /></button>
            }
          </div>
        }
      </ion-toolbar>
    </ion-header>

    <ion-content [scrollY]="false">
      <cdk-virtual-scroll-viewport itemSize="54" minBufferPx="600" maxBufferPx="1200" class="viewport">
        <div *cdkVirtualFor="let r of rows(); trackBy: trackRow" class="vrow">
          @if (r.type === 'header') {
            <div class="group">{{ r.title }} <span class="faint">· {{ r.count }}</span></div>
          } @else {
            <button type="button" class="item" role="checkbox" [attr.aria-checked]="isOn(r.label)" (click)="toggle(r.label)">
              <span class="box" [class.on]="isOn(r.label)">@if (isOn(r.label)) { <app-icon name="check" [size]="14" [stroke]="3" /> }</span>
              <span>{{ r.label }}</span>
            </button>
          }
        </div>
      </cdk-virtual-scroll-viewport>
    </ion-content>

    <ion-footer class="ion-no-border">
      <div class="foot">
        <button type="button" class="btn btn-primary btn-block" (click)="done()">
          {{ selected().length ? 'בחרתי ' + selected().length : 'סיום' }}
        </button>
      </div>
    </ion-footer>
  `,
  styles: `
    .head { display: flex; align-items: center; gap: 14px; padding: 16px var(--gutter) 8px; }
    .head h1 { font-size: 24px; }
    .search { display: flex; align-items: center; gap: 10px; margin: 6px var(--gutter) 10px; padding: 0 16px; min-height: 50px;
      border-radius: 14px; background: var(--surface); box-shadow: var(--shadow-sm); color: var(--faint); }
    .search input { border: 0; outline: 0; background: transparent; font: inherit; font-size: 16px; flex-grow: 1; color: var(--ink); }
    .picked { margin: 0; padding-bottom: 10px; gap: 8px; }
    .viewport { height: 100%; }
    .vrow { height: 54px; padding: 0 var(--gutter); box-sizing: border-box; }
    .group { height: 54px; display: flex; align-items: flex-end; padding-bottom: 8px; font-size: 15px; font-weight: 700; color: var(--muted); }
    .item { width: 100%; height: 54px; display: flex; align-items: center; gap: 14px; border: 0; border-bottom: 1px solid var(--line);
      background: transparent; color: var(--ink); font-size: 17px; font-weight: 600; text-align: start; }
    .box { width: 24px; height: 24px; border-radius: 7px; border: 2px solid #CFC2B4; display: inline-flex; align-items: center; justify-content: center; }
    .box.on { background: var(--accent); border-color: var(--accent); color: #fff; }
    .foot { padding: 12px var(--gutter) calc(16px + env(safe-area-inset-bottom)); background: var(--bg); }
  `
})
export class InterestsPickerModal {
  private readonly modalCtrl = inject(ModalController);

  readonly data = input.required<InterestsDto>();
  readonly initial = input<string[]>([]);

  protected readonly q = signal('');
  private readonly picked = signal<string[] | null>(null);
  protected readonly selected = computed(() => this.picked() ?? this.initial());

  protected readonly rows = computed<Row[]>(() => {
    const q = this.q().trim();
    const d = this.data();
    const rows: Row[] = [];
    const rec = d.recommended.filter(i => !q || i.includes(q));
    if (rec.length) {
      rows.push({ type: 'header', title: 'מומלצים בשבילה', count: rec.length });
      rec.forEach(label => rows.push({ type: 'item', label }));
    }
    for (const c of d.categories) {
      const items = c.items.filter(i => !q || i.includes(q) || c.title.includes(q));
      if (!items.length) continue;
      rows.push({ type: 'header', title: c.title, count: items.length });
      items.forEach(label => rows.push({ type: 'item', label }));
    }
    return rows;
  });

  protected trackRow = (i: number, r: Row) => (r.type === 'header' ? `h:${r.title}` : `i:${i}:${r.label}`);

  isOn(label: string) { return this.selected().includes(label); }

  toggle(label: string) {
    const cur = this.selected();
    this.picked.set(cur.includes(label) ? cur.filter(x => x !== label) : [...cur, label]);
  }

  done() { void this.modalCtrl.dismiss(this.selected(), 'select'); }
  close() { void this.modalCtrl.dismiss(null, 'cancel'); }
}
