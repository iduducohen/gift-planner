import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonContent, ViewWillEnter } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BriefStore } from '../../core/brief.store';
import { avatar, daysUntil, inDays, shortDate } from '../../core/format';
import { RecipientSummaryDto } from '../../core/models';
import { Pickers } from '../../core/pickers';
import { Icon } from '../../ui/icon';
import { Steps } from '../../ui/steps';

@Component({
  selector: 'app-start',
  imports: [IonContent, RouterLink, Icon, Steps],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content>
      <main class="page narrow">
        <div class="between">
          <app-steps [current]="1" />
          <a routerLink="/people" class="icon-btn mobile-only" aria-label="האנשים שלי"><app-icon name="people" /></a>
        </div>

        <div class="stack-sm">
          <h1>למי נכין הפתעה?</h1>
          <p class="lead">נתחיל מהבסיס. כל השאלון לוקח פחות מדקה.</p>
        </div>

        <div class="stack">
          <button type="button" class="row-link" (click)="pickRelationship()">
            <span class="ico"><app-icon name="heart" [size]="22" /></span>
            <span class="txt"><span class="k">למי המתנה</span>
              <span class="v" [class.placeholder]="!d().relationship">{{ d().recipientName ?? d().relationship?.label ?? 'בחירה מרשימה' }}</span></span>
            <app-icon name="chevron" class="chev" />
          </button>

          <button type="button" class="row-link" (click)="pickOccasion()">
            <span class="ico warm"><app-icon name="gift" [size]="22" /></span>
            <span class="txt"><span class="k">לאיזה אירוע</span>
              <span class="v" [class.placeholder]="!d().occasion">{{ d().occasion?.label ?? 'בחירה מרשימה' }}</span></span>
            <app-icon name="chevron" class="chev" />
          </button>

          <div class="row-link date-row">
            <span class="ico warm"><app-icon name="calendar" [size]="22" /></span>
            <span class="txt"><span class="k">מתי <span class="faint">(לא חובה)</span></span>
              @if (d().eventDate; as date) {
                <span class="v">{{ fmtDate(date) }} <span class="soon">· {{ fmtIn(date) }}</span></span>
              } @else {
                <span class="v placeholder">בחירת תאריך</span>
              }
            </span>
            @if (d().eventDate) {
              <button type="button" class="clear" (click)="brief.patch({ eventDate: null })" aria-label="ניקוי תאריך"><app-icon name="close" [size]="16" /></button>
            }
            <input #date type="date" class="date-input" [min]="today" [value]="d().eventDate ?? ''"
                   (change)="brief.patch({ eventDate: $any($event.target).value || null })" aria-label="תאריך האירוע" />
          </div>
        </div>

        @if (people().length) {
          <section class="stack">
            <span class="label">או מישהו ששמרת</span>
            <div class="people">
              @for (p of people(); track p.id) {
                <button type="button" class="person" (click)="usePerson(p)" [class.on]="d().recipientId === p.id">
                  <span class="av" [style.background]="av(p.nickname)[0]" [style.color]="av(p.nickname)[1]">{{ p.nickname.charAt(0) }}</span>
                  <span>{{ p.nickname }}</span>
                </button>
              }
            </div>
          </section>
        }

        <button type="button" class="btn btn-primary btn-block cta" [disabled]="!brief.canContinue()" (click)="next()">המשך</button>
      </main>
    </ion-content>
  `,
  styles: `
    .date-row { position: relative; cursor: pointer; }
    .date-input { position: absolute; inset: 0; opacity: 0; width: 100%; height: 100%; cursor: pointer; pointer-events: auto; }
    .clear { position: relative; z-index: 1; border: 0; background: var(--surface-2); width: 32px; height: 32px; border-radius: 999px; display: flex; align-items: center; justify-content: center; }
    .soon { font-size: 15px; font-weight: 600; color: var(--accent); }
    .people { display: flex; gap: 18px; overflow-x: auto; }
    .person { display: flex; flex-direction: column; align-items: center; gap: 6px; border: 0; background: transparent; color: var(--ink); font-size: 14px; font-weight: 600; }
    .av { width: 56px; height: 56px; border-radius: 999px; font-family: var(--font-head); font-size: 20px; font-weight: 600; display: flex; align-items: center; justify-content: center; }
    .person.on .av { box-shadow: 0 0 0 3px var(--bg), 0 0 0 5px var(--accent); }
    .cta { margin-top: 8px; }
    @media (min-width: 900px) { .mobile-only { display: none; } }
  `
})
export class StartPage implements ViewWillEnter {
  protected readonly brief = inject(BriefStore);
  private readonly pickers = inject(Pickers);
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);

  protected readonly d = this.brief.draft;
  protected readonly people = signal<RecipientSummaryDto[]>([]);
  protected readonly today = new Date().toISOString().slice(0, 10);
  protected readonly date = viewChild<ElementRef<HTMLInputElement>>('date');
  protected readonly av = avatar;
  protected readonly hasPeople = computed(() => this.people().length > 0);

  ionViewWillEnter() {
    this.api.recipients().subscribe({ next: p => this.people.set(p.slice(0, 6)), error: () => this.people.set([]) });
  }

  fmtDate = shortDate;
  fmtIn = (iso: string) => inDays(daysUntil(iso));

  async pickRelationship() {
    const o = await this.pickers.relationship(this.d().relationship?.key ?? null);
    if (o) this.brief.patch({ relationship: o, recipientId: null, recipientName: null });
  }

  async pickOccasion() {
    const o = await this.pickers.occasion(this.d().occasion?.key ?? null);
    if (o) this.brief.patch({ occasion: o });
  }

  async usePerson(p: RecipientSummaryDto) {
    const [full, ages] = await Promise.all([firstValueFrom(this.api.recipient(p.id)), firstValueFrom(this.api.ageRanges$)]);
    const age = ages.flatMap(g => g.options).find(o => o.key === full.ageRangeKey);
    this.brief.fromRecipient(full, age?.label ?? null);
  }

  next() { void this.router.navigateByUrl('/new/about'); }
}
