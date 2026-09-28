import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Haptics, NotificationType } from '@capacitor/haptics';

const COLORS = ['#A3223F', '#E8A0B0', '#F2C14E', '#17653F', '#7FA7D9', '#F08A5D'];

/**
 * Confetti + balloons when the gift is finally chosen. Pure CSS animations on a fixed overlay
 * (no canvas, no library); skipped entirely for users who prefer reduced motion.
 */
@Injectable({ providedIn: 'root' })
export class CelebrateService {
  private readonly doc = inject(DOCUMENT);

  celebrate(durationMs = 5200): void {
    void Haptics.notification({ type: NotificationType.Success }).catch(() => undefined);

    const reduce = this.doc.defaultView?.matchMedia?.('(prefers-reduced-motion: reduce)').matches;
    if (reduce) return;

    const layer = this.doc.createElement('div');
    layer.className = 'celebrate-layer';
    layer.setAttribute('aria-hidden', 'true');
    const width = this.doc.defaultView?.innerWidth ?? 390;

    for (let i = 0; i < 48; i++) {
      const p = this.doc.createElement('span');
      p.className = 'confetti';
      Object.assign(p.style, {
        left: `${(i * 89) % Math.max(width - 10, 50)}px`,
        width: `${7 + (i % 3) * 3}px`,
        height: `${11 + (i % 4) * 3}px`,
        borderRadius: i % 2 ? '2px' : '999px',
        background: COLORS[i % COLORS.length],
        animationDuration: `${2.2 + (i % 5) * 0.3}s`,
        animationDelay: `${(i % 10) * 0.1}s`
      });
      layer.appendChild(p);
    }

    const balloons = width > 700 ? 10 : 6;
    for (let i = 0; i < balloons; i++) {
      const b = this.doc.createElement('span');
      b.className = 'balloon';
      Object.assign(b.style, {
        left: `${((i * 67 + 10) % Math.max(width - 60, 60))}px`,
        animationDuration: `${3.8 + (i % 3) * 0.7}s`,
        animationDelay: `${i * 0.18}s`
      });
      b.innerHTML = `<span class="balloon-body" style="background:${COLORS[(i + 2) % COLORS.length]}"></span><span class="balloon-string"></span>`;
      layer.appendChild(b);
    }

    this.doc.body.appendChild(layer);
    setTimeout(() => layer.remove(), durationMs);
  }
}
