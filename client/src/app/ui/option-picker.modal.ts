import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { IonContent, IonHeader, IonToolbar, ModalController } from '@ionic/angular';
import { OptionDto, OptionGroupDto } from '../core/models';
import { Icon } from './icon';

/**
 * Single-select list with search (who is the gift for / which occasion / age range).
 * Frequent options first as large rows; the full catalogue grouped below.
 */
@Component({
  selector: 'app-option-picker',
  imports: [IonHeader, IonToolbar, IonContent, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-header class="ion-no-border">
      <ion-toolbar>
        <div class="head">
          <button class="icon-btn" type="button" (click)="close()" aria-label="סגירה"><app-icon name="close" /></button>
          <h1>{{ title() }}</h1>
        </div>
        @if (searchable()) {
          <label class="search">
            <app-icon name="search" [size]="18" />
            <input type="search" [placeholder]="searchPlaceholder()" [value]="q()" (input)="q.set($any($event.target).value)" />
          </label>
        }
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <div class="body">
        @if (!q() && common().length) {
          <section class="stack-sm">
            <span class="label">הכי נפוצים</span>
            <div class="grid">
              @for (o of common(); track o.key) {
                <button type="button" class="opt big" [class.on]="o.key === selectedKey()" (click)="pick(o)">
                  <span>{{ o.label }}</span>
                  @if (o.key === selectedKey()) { <app-icon name="check" /> }
                </button>
              }
            </div>
          </section>
        }
        @for (g of filteredGroups(); track g.title) {
          <section class="stack-sm">
            <span class="label">{{ g.title }}</span>
            <div class="list card">
              @for (o of g.options; track o.key) {
                <button type="button" class="opt" [class.on]="o.key === selectedKey()" (click)="pick(o)">
                  <span class="txt">{{ o.label }} @if (o.note) { <small>{{ o.note }}</small> }</span>
                  @if (o.key === selectedKey()) { <app-icon name="check" /> }
                </button>
              }
            </div>
          </section>
        } @empty {
          <p class="lead empty">לא מצאנו "{{ q() }}". נסו מילה אחרת.</p>
        }
      </div>
    </ion-content>
  `,
  styles: `
    .head { display: flex; align-items: center; gap: 14px; padding: 16px var(--gutter) 8px; }
    .head h1 { font-size: 24px; }
    .search { display: flex; align-items: center; gap: 10px; margin: 6px var(--gutter) 12px; padding: 0 16px; min-height: 50px;
      border-radius: 14px; background: var(--surface); box-shadow: var(--shadow-sm); color: var(--faint); }
    .search input { border: 0; outline: 0; background: transparent; font: inherit; font-size: 16px; flex-grow: 1; color: var(--ink); }
    .body { padding: 8px var(--gutter) 40px; display: flex; flex-direction: column; gap: 24px; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    .list { overflow: hidden; }
    .opt { display: flex; align-items: center; justify-content: space-between; gap: 10px; width: 100%; min-height: 54px; padding: 0 18px;
      border: 0; background: transparent; color: var(--ink); font-size: 17px; font-weight: 600; text-align: start; }
    .list .opt + .opt { border-top: 1px solid var(--line); }
    .opt.big { min-height: 60px; border-radius: 16px; background: var(--surface); box-shadow: var(--shadow-sm); font-weight: 700; }
    .opt.on { color: var(--accent); }
    .opt.big.on { background: var(--accent-soft); box-shadow: inset 0 0 0 2px var(--accent); }
    .txt small { display: block; font-size: 13px; font-weight: 400; color: var(--muted); }
    .empty { text-align: center; padding: 32px 0; }
  `
})
export class OptionPickerModal {
  private readonly modalCtrl = inject(ModalController);

  readonly title = input('בחירה');
  readonly common = input<OptionDto[]>([]);
  readonly groups = input<OptionGroupDto[]>([]);
  readonly selectedKey = input<string | null>(null);
  readonly searchable = input(true);
  readonly searchPlaceholder = input('חיפוש');

  protected readonly q = signal('');

  protected readonly filteredGroups = computed(() => {
    const q = this.q().trim();
    if (!q) return this.groups();
    const all = [...this.common(), ...this.groups().flatMap(g => g.options)];
    const seen = new Set<string>();
    const hits = all.filter(o => o.label.includes(q) && !seen.has(o.key) && seen.add(o.key));
    return hits.length ? [{ title: 'תוצאות', options: hits }] : [];
  });

  pick(o: OptionDto) { void this.modalCtrl.dismiss(o, 'select'); }
  close() { void this.modalCtrl.dismiss(null, 'cancel'); }
}
