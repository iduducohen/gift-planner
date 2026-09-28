import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Line icons from the design (24×24, stroke = currentColor). Keys match CatalogIdea.IconKey on the server. */
export const ICONS: Record<string, string> = {
  sunset: 'M3 18h18M6 18a6 6 0 0 1 12 0M12 6V4M5.6 8.6 4.2 7.2M18.4 8.6l1.4-1.4',
  gift: 'M3 8h18v4H3zM12 8v13M5 12v9h14v-9M12 8c-2-4-6-4-6-1.5S10 8 12 8zm0 0c2-4 6-4 6-1.5S14 8 12 8z',
  frame: 'M4 4h16v16H4zM4 16l5-5 4 4 3-3 4 4',
  spa: 'M12 3s6 6.5 6 11a6 6 0 0 1-12 0c0-4.5 6-11 6-11z',
  camera: 'M4 8h3l2-3h6l2 3h3v11H4zM12 17a4 4 0 1 0 0-8 4 4 0 0 0 0 8z',
  cup: 'M5 8h12v6a6 6 0 0 1-12 0zM17 10h2a2 2 0 0 1 0 4h-2',
  coffee: 'M5 8h12v6a6 6 0 0 1-12 0zM17 10h2a2 2 0 0 1 0 4h-2M8 3v2M12 3v2',
  plate: 'M12 4a8 8 0 1 0 0 16 8 8 0 0 0 0-16zM12 8a4 4 0 1 0 0 8 4 4 0 0 0 0-8z',
  flower: 'M12 13a3 3 0 1 0 0-6 3 3 0 0 0 0 6zM12 7V4M9 10H6M18 10h-3M12 13v8M12 17c-2 0-4-1-5-3',
  wine: 'M8 3h8l-1 6a3 3 0 0 1-6 0zM12 12v8M8 21h8',
  mic: 'M9 3h6v10H9zM5 11a7 7 0 0 0 14 0M12 18v3',
  ring: 'M12 9a6 6 0 1 0 0 12 6 6 0 0 0 0-12zM9 3h6l-3 6z',
  bed: 'M3 18V8M3 14h18v4M21 18v-6a3 3 0 0 0-3-3h-7v5M7 11a1.5 1.5 0 1 0 0-.01',
  boat: 'M3 16l2 4h14l2-4zM12 3v13M12 4l6 9h-6',
  ticket: 'M3 8a2 2 0 0 0 0 4v4h18v-4a2 2 0 0 0 0-4V4H3zM14 4v16',
  heart: 'M12 20s-7-4.4-7-10a4 4 0 0 1 7-2.6A4 4 0 0 1 19 10c0 5.6-7 10-7 10z',
  candle: 'M9 10h6v11H9zM12 10V7M12 3c1 1.5 1 2.5 0 4-1-1.5-1-2.5 0-4z',
  book: 'M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3zM5 17a3 3 0 0 1 3-3h11',
  jeep: 'M3 15V11l2-4h9l3 4h4v4M7 18a2 2 0 1 0 0-.01M17 18a2 2 0 1 0 0-.01',
  bag: 'M5 8h14l-1 12H6zM9 8V6a3 3 0 0 1 6 0v2',
  ball: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zM3.5 10h17M3.5 14h17M12 3c3 3 3 15 0 18M12 3c-3 3-3 15 0 18',
  headphones: 'M4 15v-3a8 8 0 0 1 16 0v3M4 15h3v5H4zM17 15h3v5h-3z',
  puzzle: 'M4 8h4a2 2 0 1 1 4 0h4v4a2 2 0 1 1 0 4v4H4z',
  plant: 'M12 21v-8M12 13c0-4 3-7 7-7 0 4-3 7-7 7zM12 15c0-3-2-5-6-5 0 3 2 5 6 5zM8 21h8',
  sneaker: 'M3 16h18v3H3zM3 16l1-6 5 1 3 2 6 1 3 2',
  game: 'M6 9h12a4 4 0 0 1 0 8l-3-2H9l-3 2a4 4 0 0 1 0-8zM8 11v3M6.5 12.5h3M16 12h.01M18 13h.01',
  lego: 'M4 10h16v10H4zM7 7h3v3H7zM14 7h3v3h-3z',
  rocket: 'M12 3c3 2 5 6 5 10l-2 3H9l-2-3c0-4 2-8 5-10zM12 10a1.5 1.5 0 1 0 0-.01M9 16l-2 4M15 16l2 4',
  dino: 'M4 18h3l1-4h6l1 4h3M8 14c-3 0-4-3-4-5M14 14l3-6h3l1 2-3 1',
  star: 'M12 3l2.6 5.6 6 .6-4.5 4 1.3 6-5.4-3.2L6.6 19.2l1.3-6-4.5-4 6-.6z',
  sparkle: 'M12 3v5M12 16v5M3 12h5M16 12h5M6 6l3 3M15 15l3 3M18 6l-3 3M9 15l-3 3',
  smile: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zM8 14c1 1.5 2.4 2 4 2s3-.5 4-2M9 9.5h.01M15 9.5h.01',
  people: 'M9 11.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7zM2.5 20c.8-3.5 3.4-5.5 6.5-5.5s5.7 2 6.5 5.5M16 4.8a3.5 3.5 0 0 1 0 6.4M18 14.8c1.8.7 3 2.5 3.5 5.2',
  bookmark: 'M6 3h12v18l-6-4-6 4z',
  home: 'M3 11l9-8 9 8M5 9v11h14V9',
  calendar: 'M3.5 5h17v15h-17zM8 3v4M16 3v4M3.5 10h17',
  chevron: 'M15 6l-6 6 6 6',
  back: 'M5 12h14M13 6l6 6-6 6',
  close: 'M6 6l12 12M18 6L6 18',
  check: 'M5 12l5 5L20 7',
  plus: 'M12 5v14M5 12h14',
  search: 'M11 4a7 7 0 1 0 0 14 7 7 0 0 0 0-14zM20 20l-4-4',
  edit: 'M4 20h4L19 9l-4-4L4 16z',
  tag: 'M3 12l9-9h8v8l-9 9zM15.5 8.5h.01',
  sliders: 'M4 7h10M18 7h2M4 17h4M12 17h8M16 5v4M10 15v4',
  external: 'M14 4h6v6M20 4l-9 9M18 14v6H4V6h6',
  trash: 'M4 7h16M9 7V4h6v3M6 7l1 13h10l1-13',
  user: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zM4 21c1-4 4-6 8-6s7 2 8 6',
  bell: 'M6 16V11a6 6 0 0 1 12 0v5l2 2H4zM10 20h4',
  ban: 'M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18zM5.6 5.6l12.8 12.8'
};

@Component({
  selector: 'app-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'app-icon', 'aria-hidden': 'true' },
  template: `
    <svg [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none" stroke="currentColor"
         [attr.stroke-width]="stroke()" stroke-linecap="round" stroke-linejoin="round">
      <path [attr.d]="path()" />
    </svg>`,
  styles: `:host{display:inline-flex;line-height:0;flex-shrink:0}`
})
export class Icon {
  readonly name = input.required<string>();
  readonly size = input(20);
  readonly stroke = input(1.8);
  protected readonly path = computed(() => ICONS[this.name()] ?? ICONS['gift']);
}
