import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { dealLabel, hasDeal, priceLabel, shekel, tint } from '../core/format';
import { IdeaDto } from '../core/models';
import { Icon } from './icon';

/** Idea card: "big" for the top carousel, "small" for the grid. Images are lazy (partner photo slot). */
@Component({
  selector: 'app-idea-card',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '[class.big]': 'variant() === "big"' },
  template: `
    <button type="button" class="card-btn" (click)="open.emit(idea())">
      <div class="media" [style.background]="colors()[0]" [style.color]="colors()[1]">
        <app-icon [name]="idea().iconKey" [size]="variant() === 'big' ? 44 : 34" [stroke]="1.3" />
        @if (deal()) { <span class="deal-badge">{{ dealText() }}</span> }
      </div>
      <div class="body">
        @if (variant() === 'big') { <span class="kind">{{ idea().kindLabel }}</span> }
        <span class="title">{{ idea().title }}</span>
        @if (variant() === 'big') { <span class="why">{{ idea().why }}</span> }
        <span class="price">
          <b>{{ price() }}</b>
          @if (deal() && idea().listPrice) { <s>{{ was() }}</s> }
        </span>
      </div>
    </button>
    <button type="button" class="save" [class.on]="idea().saved" (click)="toggleSave.emit(idea())"
            [attr.aria-label]="idea().saved ? 'הסרה מהשמורים' : 'שמירה'" [attr.aria-pressed]="idea().saved">
      <app-icon name="heart" [size]="18" />
    </button>
  `,
  styles: `
    :host { position: relative; display: block; border-radius: 20px; background: var(--surface); box-shadow: var(--shadow-sm); overflow: hidden; }
    :host(.big) { flex-shrink: 0; width: 290px; border-radius: 24px; box-shadow: var(--shadow-lg); }
    .card-btn { display: flex; flex-direction: column; width: 100%; padding: 0; border: 0; background: transparent; color: var(--ink); text-align: start; }
    .media { position: relative; height: 128px; display: flex; align-items: center; justify-content: center; }
    :host(.big) .media { height: 190px; }
    .deal-badge { position: absolute; top: 10px; inset-inline-start: 10px; }
    :host(.big) .deal-badge { top: 14px; inset-inline-start: 14px; min-height: 28px; padding: 0 10px; font-size: 13px; }
    .body { padding: 12px 14px 14px; display: flex; flex-direction: column; gap: 4px; }
    :host(.big) .body { padding: 16px 18px 18px; gap: 6px; }
    .kind { font-size: 13px; font-weight: 700; color: var(--accent); }
    .title { font-size: 15px; font-weight: 700; line-height: 1.35; min-height: 2.7em; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
    :host(.big) .title { font-family: var(--font-head); font-size: 19px; font-weight: 600; line-height: 1.3; min-height: 0; }
    .why { font-size: 15px; line-height: 1.45; color: var(--muted); display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
    .price { display: flex; align-items: baseline; gap: 8px; }
    .price b { font-size: 15px; color: var(--ink-2); }
    :host(.big) .price { margin-top: 6px; }
    :host(.big) .price b { font-size: 19px; color: var(--ink); }
    .price s { font-size: 14px; color: var(--faint); }
    .save { position: absolute; top: 10px; inset-inline-end: 10px; width: 36px; height: 36px; border-radius: 999px; border: 0;
      background: #fff; color: var(--ink); display: flex; align-items: center; justify-content: center; }
    .save.on { color: var(--accent); }
    .save.on ::ng-deep path { fill: currentColor; }
  `
})
export class IdeaCard {
  readonly idea = input.required<IdeaDto>();
  readonly variant = input<'big' | 'small'>('small');
  readonly open = output<IdeaDto>();
  readonly toggleSave = output<IdeaDto>();

  protected readonly colors = computed(() => tint(this.idea().kind));
  protected readonly deal = computed(() => hasDeal(this.idea()));
  protected readonly dealText = computed(() => dealLabel(this.idea()));
  protected readonly price = computed(() => priceLabel(this.idea()));
  protected readonly was = computed(() => shekel(this.idea().listPrice ?? 0));
}
