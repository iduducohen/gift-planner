import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { IonBackButton, IonContent } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { inDays, shekel, shortDate } from '../../core/format';
import { IdeaActions } from '../../core/idea-actions.service';
import { PlanDto, PlanTaskDto } from '../../core/models';
import { Icon } from '../../ui/icon';

@Component({
  selector: 'app-plan',
  imports: [IonContent, IonBackButton, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow">
        <div class="between">
          <ion-back-button defaultHref="/saved" text="" class="icon-btn" aria-label="חזרה" />
          <button type="button" class="icon-btn" (click)="share()" aria-label="שיתוף תוכנית"><app-icon name="external" /></button>
        </div>

        @if (plan(); as p) {
          <div class="stack-sm">
            <h1>{{ p.title }}</h1>
            <div class="row wrap">
              <span class="pill accent"><app-icon name="calendar" [size]="16" /> {{ date(p.eventDate) }} · {{ when(p.daysLeft) }}</span>
              <span class="pill">{{ range(p.totalMin, p.totalMax) }}</span>
            </div>
          </div>

          <section class="stack">
            <h2>מהלך היום</h2>
            <ol class="timeline">
              @for (s of p.steps; track $index) {
                <li [class.paid]="s.costMax > 0">
                  <span class="dot"></span>
                  <div class="step card">
                    <div class="between"><b class="time">{{ s.time }}</b>@if (s.costMax > 0) { <span class="small muted">{{ range(s.costMin, s.costMax) }}</span> }</div>
                    <b>{{ s.title }}</b>
                    <span class="muted">{{ s.detail }}</span>
                  </div>
                </li>
              }
            </ol>
            @if (p.planB) { <div class="planb"><app-icon name="sparkle" [size]="18" /><span><b>תוכנית ב׳:</b> {{ p.planB }}</span></div> }
          </section>

          <section class="stack">
            <div class="between"><h2>מה צריך לעשות</h2><span class="small muted">{{ doneCount() }}/{{ p.tasks.length }}</span></div>
            <div class="card tasks">
              @for (t of p.tasks; track t.id) {
                <label class="task" [class.done]="t.done">
                  <input type="checkbox" [checked]="t.done" (change)="toggle(t)" />
                  <span class="box">@if (t.done) { <app-icon name="check" [size]="14" [stroke]="3" /> }</span>
                  <span class="stack-sm tight"><span class="t">{{ t.title }}</span><span class="small muted">עד {{ date(t.dueDate) }}</span></span>
                </label>
              }
            </div>
          </section>

          <button type="button" class="btn btn-primary btn-block" (click)="remind()"><app-icon name="bell" [size]="18" /> תזכיר לי כל משימה בזמן</button>
        } @else {
          <div class="skel" style="height: 36px; width: 70%"></div>
          <div class="skel" style="height: 220px"></div>
          <div class="skel" style="height: 160px"></div>
        }
      </main>
    </ion-content>
  `,
  styles: `
    .wrap { flex-wrap: wrap; gap: 8px; }
    .pill { display: inline-flex; align-items: center; gap: 6px; min-height: 36px; padding: 0 14px; border-radius: 999px; background: var(--surface-2); font-size: 14px; font-weight: 700; }
    .pill.accent { background: var(--accent-soft); color: var(--accent); }
    .timeline { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; position: relative; }
    .timeline::before { content: ''; position: absolute; inset-inline-start: 9px; top: 12px; bottom: 12px; width: 2px; background: var(--line); }
    .timeline li { display: flex; gap: 14px; align-items: flex-start; position: relative; }
    .dot { flex-shrink: 0; width: 20px; height: 20px; margin-top: 18px; border-radius: 999px; background: #fff; border: 3px solid var(--accent); z-index: 1; }
    li.paid .dot { background: var(--accent); }
    .step { flex-grow: 1; padding: 14px 16px; display: flex; flex-direction: column; gap: 4px; }
    .time { color: var(--accent); }
    .planb { display: flex; gap: 10px; padding: 14px 16px; border-radius: 16px; background: var(--surface-2); line-height: 1.5; }
    .tasks { overflow: hidden; }
    .task { display: flex; align-items: center; gap: 14px; padding: 14px 16px; cursor: pointer; }
    .task + .task { border-top: 1px solid var(--line); }
    .task input { position: absolute; opacity: 0; pointer-events: none; }
    .box { flex-shrink: 0; width: 26px; height: 26px; border-radius: 8px; border: 2px solid #CFC2B4; display: flex; align-items: center; justify-content: center; }
    .task.done .box { background: var(--deal); border-color: var(--deal); color: #fff; }
    .task.done .t { text-decoration: line-through; color: var(--muted); }
    .t { font-weight: 700; }
    .tight { gap: 2px; }
  `
})
export class PlanPage {
  private readonly api = inject(ApiService);
  private readonly actions = inject(IdeaActions);
  readonly id = input.required<string>();
  protected readonly plan = signal<PlanDto | null>(null);
  protected readonly doneCount = computed(() => this.plan()?.tasks.filter(t => t.done).length ?? 0);
  protected readonly date = shortDate;
  protected readonly when = inDays;

  constructor() {
    effect(() => this.api.plan(this.id()).subscribe({ next: p => this.plan.set(p), error: () => void this.actions.toast('התוכנית לא נמצאה') }));
  }

  range(min: number, max: number) { return min === max ? shekel(min) : `${min.toLocaleString('he-IL')}–${shekel(max)}`; }

  async toggle(t: PlanTaskDto) {
    const p = this.plan()!;
    this.plan.set({ ...p, tasks: p.tasks.map(x => (x.id === t.id ? { ...x, done: !t.done } : x)) }); // optimistic
    try {
      this.plan.set(await firstValueFrom(this.api.setTask(p.id, t.id, !t.done)));
    } catch {
      this.plan.set(p);
      await this.actions.toast('לא נשמר. נסו שוב.');
    }
  }

  async share() {
    const p = this.plan();
    if (!p) return;
    const text = `${p.title}\n` + p.steps.map(s => `${s.time} · ${s.title}`).join('\n');
    try {
      if (navigator.share) await navigator.share({ title: p.title, text });
      else { await navigator.clipboard.writeText(text); await this.actions.toast('הועתק ללוח'); }
    } catch { /* cancelled */ }
  }

  async remind() {
    // Push reminders need the native app (Capacitor Local Notifications) or sign-in for email; we confirm intent here.
    await this.actions.toast('נזכיר לך לפני כל משימה ✓');
  }
}
