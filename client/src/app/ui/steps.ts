import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** The three-dash progress indicator at the top of the brief wizard. */
@Component({
  selector: 'app-steps',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { role: 'progressbar', '[attr.aria-valuenow]': 'current()', 'aria-valuemin': '1', '[attr.aria-valuemax]': 'total()', 'aria-label': 'שלב' },
  template: `@for (i of dots(); track i) { <span [class.on]="i <= current()"></span> }`,
  styles: `:host{display:flex;gap:6px} span{width:28px;height:6px;border-radius:3px;background:var(--line)} span.on{background:var(--accent)}`
})
export class Steps {
  readonly current = input(1);
  readonly total = input(3);
  protected dots = () => Array.from({ length: this.total() }, (_, i) => i + 1);
}
