import {
  ChangeDetectionStrategy, Component, DestroyRef, ElementRef, computed, effect, inject, input, signal, untracked, viewChild, viewChildren
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IonContent, ModalController } from '@ionic/angular';
import { injectVirtualizer } from '@tanstack/angular-virtual';
import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { BriefStore } from '../../core/brief.store';
import { pronouns } from '../../core/format';
import { IdeaActions } from '../../core/idea-actions.service';
import { IdeaDto, IdeaSetDto } from '../../core/models';
import { Icon } from '../../ui/icon';
import { IdeaCard } from '../../ui/idea-card';
import { IdeaDetail } from '../../ui/idea-detail';
import { RefineChoice, RefineModal } from '../../ui/refine.modal';

const PAGE = 8;
const FILTERS = [
  { key: 'all', label: 'הכול' }, { key: 'experience', label: 'חוויות' }, { key: 'product', label: 'מתנות' },
  { key: 'pamper', label: 'פינוק' }, { key: 'personal', label: 'אישי' }, { key: 'bundle', label: 'חבילות' }, { key: 'deal', label: 'עם הנחה' }
];

/**
 * Results: 5 best (carousel) + the rest in a virtualized, lazily-paged grid.
 * Only visible rows are in the DOM (@tanstack/angular-virtual); the next page is fetched when
 * the user nears the end, with skeletons meanwhile. The "load more" button stays as a fallback.
 */
@Component({
  selector: 'app-ideas',
  imports: [IonContent, RouterLink, Icon, IdeaCard],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ion-content #content [scrollEvents]="false">
      <main class="page wide">
        <div class="stack">
          <div class="between">
            <button type="button" class="icon-btn" (click)="back()" aria-label="חזרה"><app-icon name="back" /></button>
            <a routerLink="/new/about" class="summary">{{ brief.summary() }} <app-icon name="edit" [size]="14" /></a>
          </div>
          <h1>@if (set()) { מצאנו {{ set()!.total }} רעיונות {{ p().forHer }} } @else { מחפשים רעיונות… }</h1>
          @if (set()?.refineLabel) { <span class="refined"><app-icon name="sliders" [size]="16" /> כיוון: {{ set()!.refineLabel }}</span> }
        </div>

        <section class="stack">
          <div class="between"><h2>{{ top().length }} הכי מתאימים</h2><span class="small muted mobile-only">החליקו לצד</span></div>
          <div class="hscroll top">
            @for (i of top(); track i.id) {
              <app-idea-card [idea]="i" variant="big" (open)="openIdea($event)" (toggleSave)="toggleSave($event)" />
            } @empty {
              @for (s of [1, 2, 3]; track s) { <div class="skel top-skel"></div> }
            }
          </div>
          <div class="deal-note"><app-icon name="tag" [size]="18" /> תגית ירוקה = הנחה בלעדית מעסקים שותפים. הדירוג לא מושפע ממנה.</div>
        </section>

        <section class="stack" #gridSection>
          <h2>עוד {{ total() }} רעיונות</h2>
          <div class="hscroll filters" role="toolbar" aria-label="סינון">
            @for (f of filters; track f.key) {
              <button type="button" class="chip" [attr.aria-pressed]="filter() === f.key" (click)="setFilter(f.key)">
                {{ f.label }}@if (f.key === 'all') { · {{ allTotal() }} }
              </button>
            }
          </div>

          <div class="vgrid" [style.height.px]="virtualizer.getTotalSize()">
            @for (row of virtualizer.getVirtualItems(); track row.key) {
              <div #vrow class="vrow" [attr.data-index]="row.index" [style.transform]="'translateY(' + (row.start - virtualizer.options().scrollMargin) + 'px)'"
                   [style.grid-template-columns]="'repeat(' + cols() + ', minmax(0, 1fr))'">
                @for (i of rowItems(row.index); track i.id) {
                  <app-idea-card [idea]="i" (open)="openIdea($event)" (toggleSave)="toggleSave($event)" />
                }
              </div>
            }
          </div>

          @if (loading()) {
            <div class="skel-grid" [style.grid-template-columns]="'repeat(' + cols() + ', minmax(0, 1fr))'">
              @for (s of skeletons(); track s) {
                <div class="skel-card"><div class="skel" style="height: 128px; border-radius: 0"></div>
                  <div class="skel" style="height: 12px; margin: 0 14px"></div><div class="skel" style="height: 12px; width: 45%; margin: 0 14px"></div></div>
              }
            </div>
          }
          @if (!loading() && total() === 0) { <p class="muted center">אין רעיונות בסינון הזה. נסו סינון אחר.</p> }
          @if (total() > 0) { <span class="small muted center">מוצגים {{ items().length }} מתוך {{ total() }}</span> }
          @if (cursor() && !loading()) {
            <button type="button" class="btn" (click)="loadMore()">טען עוד {{ nextCount() }} רעיונות</button>
          }
        </section>
      </main>

      <button type="button" class="btn btn-dark refine" (click)="refine()" [disabled]="refining()">
        <app-icon name="sliders" [size]="18" /> {{ refining() ? 'מחפשים כיוון חדש…' : 'לא בדיוק זה? שנה כיוון' }}
      </button>
    </ion-content>
  `,
  styles: `
    .page.wide { max-width: 1180px; padding-bottom: 140px; }
    .summary { display: inline-flex; align-items: center; gap: 6px; min-height: 40px; padding: 0 14px; border-radius: 999px; background: var(--surface-2); color: var(--ink); font-size: 14px; font-weight: 700; text-decoration: none; }
    h1 { font-size: 28px; }
    .refined { align-self: flex-start; display: inline-flex; align-items: center; gap: 6px; min-height: 32px; padding: 0 12px; border-radius: 999px; background: var(--accent-soft); color: var(--accent); font-size: 14px; font-weight: 700; }
    .top { gap: 14px; }
    .top-skel { flex-shrink: 0; width: 290px; height: 360px; border-radius: 24px; }
    .filters { gap: 8px; padding-bottom: 4px; }
    .vgrid { position: relative; width: 100%; }
    .vrow { position: absolute; top: 0; inset-inline: 0; display: grid; gap: 14px; padding-bottom: 14px; }
    .skel-grid { display: grid; gap: 14px; }
    .skel-card { display: flex; flex-direction: column; gap: 10px; padding-bottom: 14px; border-radius: 20px; background: #fff; overflow: hidden; }
    .center { align-self: center; text-align: center; }
    .refine { position: fixed; bottom: calc(24px + env(safe-area-inset-bottom)); left: 50%; transform: translateX(-50%); z-index: 10; min-height: 52px; white-space: nowrap; }
    @media (min-width: 900px) { .mobile-only { display: none; } }
    @media (min-width: 1100px) {
      .top { display: grid; grid-template-columns: repeat(5, minmax(0, 1fr)); overflow: visible; margin: 0; padding: 4px 0 18px; }
      .top app-idea-card { width: auto; }
    }
  `
})
export class IdeasPage {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly modals = inject(ModalController);
  private readonly actions = inject(IdeaActions);
  protected readonly brief = inject(BriefStore);

  readonly sessionId = input.required<string>();
  readonly setId = input.required<string>();

  private readonly content = viewChild.required(IonContent);
  private readonly gridSection = viewChild.required<ElementRef<HTMLElement>>('gridSection');
  private readonly rowEls = viewChildren<ElementRef<HTMLElement>>('vrow');

  protected readonly filters = FILTERS;
  protected readonly set = signal<IdeaSetDto | null>(null);
  protected readonly top = computed(() => this.set()?.top ?? []);
  protected readonly items = signal<IdeaDto[]>([]);
  protected readonly cursor = signal<string | null>(null);
  protected readonly total = signal(0);
  protected readonly allTotal = computed(() => this.set()?.more.total ?? 0);
  protected readonly filter = signal('all');
  protected readonly loading = signal(false);
  protected readonly refining = signal(false);
  protected readonly width = signal(typeof window === 'undefined' ? 390 : window.innerWidth);
  protected readonly cols = computed(() => (this.width() >= 1100 ? 4 : this.width() >= 700 ? 3 : 2));
  protected readonly rowCount = computed(() => Math.ceil(this.items().length / this.cols()));
  protected readonly nextCount = computed(() => Math.min(PAGE, this.total() - this.items().length));
  protected readonly skeletons = computed(() => Array.from({ length: this.cols() * 2 }, (_, i) => i));
  protected readonly p = computed(() => pronouns(this.brief.draft().relationship?.key));

  private readonly scrollEl = signal<HTMLElement | undefined>(undefined);
  private readonly scrollMargin = signal(0);

  protected readonly virtualizer = injectVirtualizer(() => ({
    scrollElement: this.scrollEl(),
    count: this.rowCount(),
    estimateSize: () => (this.width() >= 700 ? 262 : 232),
    overscan: 3,
    scrollMargin: this.scrollMargin(),
    measureElement: (el: Element) => el.getBoundingClientRect().height
  }));

  constructor() {
    // Prefer the set handed over by navigation (no extra request); otherwise fetch it (refresh / deep link).
    const handed = this.router.getCurrentNavigation()?.extras.state?.['set'] as IdeaSetDto | undefined;
    effect(() => {
      const id = this.setId();
      untracked(() => {
        if (handed && handed.id === id) this.apply(handed);
        else this.api.ideaSet(id).subscribe({ next: s => this.apply(s), error: () => void this.actions.toast('לא מצאנו את הרעיונות') });
      });
    });

    // Rows are measured after render, so wrapped titles never overlap the next row.
    effect(() => this.rowEls().forEach(el => this.virtualizer.measureElement(el.nativeElement)));

    // Infinite loading: when the last virtual row becomes visible, fetch the next page.
    effect(() => {
      const rows = this.virtualizer.getVirtualItems();
      const last = rows[rows.length - 1];
      if (last && last.index >= this.rowCount() - 1 && untracked(() => this.cursor() && !this.loading())) void this.loadMore();
    });

    const onResize = () => {
      this.width.set(window.innerWidth);
      this.measureMargin();
    };
    window.addEventListener('resize', onResize, { passive: true });
    inject(DestroyRef).onDestroy(() => window.removeEventListener('resize', onResize));
  }

  async ionViewDidEnter() {
    this.scrollEl.set(await this.content().getScrollElement());
    this.measureMargin();
  }

  private measureMargin() {
    // Offset of the grid inside the scroll container, so the virtualizer maps scrollTop to rows correctly.
    requestAnimationFrame(() => {
      const scroller = this.scrollEl();
      const grid = this.gridSection().nativeElement.querySelector('.vgrid');
      if (!scroller || !grid) return;
      const offset = grid.getBoundingClientRect().top - scroller.getBoundingClientRect().top + scroller.scrollTop;
      this.scrollMargin.set(Math.round(offset));
    });
  }

  private apply(s: IdeaSetDto) {
    this.set.set(s);
    this.filter.set('all');
    this.items.set(s.more.items);
    this.cursor.set(s.more.nextCursor);
    this.total.set(s.more.total);
    setTimeout(() => this.measureMargin(), 50);
  }

  protected rowItems(row: number): IdeaDto[] {
    const c = this.cols();
    return this.items().slice(row * c, row * c + c);
  }

  async setFilter(key: string) {
    if (key === this.filter()) return;
    this.filter.set(key);
    this.items.set([]);
    this.cursor.set(null);
    this.loading.set(true);
    try {
      const page = await firstValueFrom(this.api.ideasPage(this.setId(), null, PAGE, key));
      this.items.set(page.items);
      this.cursor.set(page.nextCursor);
      this.total.set(page.total);
    } finally {
      this.loading.set(false);
    }
  }

  async loadMore() {
    const cursor = this.cursor();
    if (!cursor || this.loading()) return;
    this.loading.set(true);
    try {
      const page = await firstValueFrom(this.api.ideasPage(this.setId(), cursor, PAGE, this.filter()));
      this.items.update(list => [...list, ...page.items]);
      this.cursor.set(page.nextCursor);
      this.total.set(page.total);
    } catch {
      await this.actions.toast('לא הצלחנו לטעון עוד רעיונות');
    } finally {
      this.loading.set(false);
    }
  }

  async openIdea(i: IdeaDto) {
    if (this.width() < 900) {
      await this.router.navigate(['/idea', i.id]);
      return;
    }
    // Desktop: the detail slides in as a drawer, keeping the results in place.
    const m = await this.modals.create({ component: IdeaDetailDrawer, componentProps: { ideaId: i.id }, cssClass: 'drawer' });
    await m.present();
    const { data } = await m.onWillDismiss<{ id: string; saved: boolean }[]>();
    data?.forEach(c => this.markSaved(c.id, c.saved));
  }

  async toggleSave(i: IdeaDto) {
    const saved = await this.actions.toggleSave(i);
    this.markSaved(i.id, saved);
  }

  private markSaved(id: string, saved: boolean) {
    const patch = (x: IdeaDto) => (x.id === id ? { ...x, saved } : x);
    this.items.update(list => list.map(patch));
    this.set.update(s => (s ? { ...s, top: s.top.map(patch) } : s));
  }

  async refine() {
    const m = await this.modals.create({ component: RefineModal, cssClass: 'sheet' });
    await m.present();
    const { data, role } = await m.onWillDismiss<RefineChoice>();
    if (role !== 'select' || !data) return;
    this.refining.set(true);
    try {
      const s = await firstValueFrom(this.api.generate(this.sessionId(), { parentSetId: this.setId(), refinePreset: data.preset, refineText: data.text }));
      await this.content().scrollToTop(300);
      await this.router.navigate(['/session', this.sessionId(), 'ideas', s.id], { state: { set: s }, replaceUrl: true });
      this.apply(s);
    } catch {
      await this.actions.toast('לא הצלחנו לשנות כיוון. נסו שוב.');
    } finally {
      this.refining.set(false);
    }
  }

  back() { void this.router.navigateByUrl('/new/budget'); }
}

/** Desktop drawer wrapper around the shared detail view. */
@Component({
  selector: 'app-idea-detail-drawer',
  imports: [IonContent, IdeaDetail],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ion-content><app-idea-detail [ideaId]="ideaId()" [inDrawer]="true" (closed)="close()" (savedChange)="changes.push($event)" /></ion-content>`
})
export class IdeaDetailDrawer {
  private readonly modalCtrl = inject(ModalController);
  readonly ideaId = input.required<string>();
  protected readonly changes: { id: string; saved: boolean }[] = [];
  close() { void this.modalCtrl.dismiss(this.changes); }
}
