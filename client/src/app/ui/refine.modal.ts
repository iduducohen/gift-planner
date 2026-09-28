import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ModalController } from '@ionic/angular';
import { ApiService } from '../core/api.service';
import { Icon } from './icon';

export interface RefineChoice { preset?: string; text?: string; }

/** "לא בדיוק זה? שנה כיוון" — one tap presets, or a sentence like "כבר יש לה הרבה בשמים". */
@Component({
  selector: 'app-refine',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'ion-page' },
  template: `
    <div class="sheet-scroll">
      <div class="sheet-body">
        <div class="between">
          <h1>לאן לכוון?</h1>
          <button type="button" class="icon-btn" (click)="close()" aria-label="סגירה"><app-icon name="close" /></button>
        </div>
        <div class="grid">
          @for (o of options() ?? []; track o.key) {
            <button type="button" class="opt" (click)="pick({ preset: o.key })">{{ o.label }}</button>
          }
        </div>
        <form class="stack-sm" (submit)="$event.preventDefault(); submitText()">
          <label class="label" for="refine-text">או במילים שלך</label>
          <textarea id="refine-text" class="field" rows="2" maxlength="200" placeholder="למשל: כבר יש לה הרבה בשמים, משהו יותר יצירתי"
                    [value]="text()" (input)="text.set($any($event.target).value)"></textarea>
          <button type="submit" class="btn btn-dark btn-block" [disabled]="!text().trim()">שנה כיוון</button>
        </form>
      </div>
    </div>
  `,
  styles: `
    .sheet-body { padding: 24px var(--gutter) 32px; display: flex; flex-direction: column; gap: 20px; }
    h1 { font-size: 26px; }
    .grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px; }
    .opt { min-height: 58px; border-radius: 16px; border: 0; background: var(--surface); box-shadow: var(--shadow-sm); color: var(--ink); font-size: 16px; font-weight: 700; }
    .opt:active { background: var(--accent-soft); }
  `
})
export class RefineModal {
  private readonly modalCtrl = inject(ModalController);
  protected readonly options = toSignal(inject(ApiService).refineOptions$);
  protected readonly text = signal('');

  pick(c: RefineChoice) { void this.modalCtrl.dismiss(c, 'select'); }
  submitText() { this.pick({ text: this.text().trim() }); }
  close() { void this.modalCtrl.dismiss(null, 'cancel'); }
}
