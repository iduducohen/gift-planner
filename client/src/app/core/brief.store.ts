import { Injectable, computed, effect, signal } from '@angular/core';
import { CreateSessionRequest, EmotionKey, OptionDto, RecipientDto } from './models';

export interface BriefDraft {
  recipientId: string | null;
  recipientName: string | null;
  relationship: OptionDto | null;
  occasion: OptionDto | null;
  eventDate: string | null;
  ageRange: OptionDto | null;
  interests: string[];
  avoid: string[];
  notes: string;
  budget: number;
  emotions: EmotionKey[];
}

const EMPTY: BriefDraft = {
  recipientId: null, recipientName: null, relationship: null, occasion: null, eventDate: null, ageRange: null,
  interests: [], avoid: [], notes: '', budget: 400, emotions: []
};

const KEY = 'gp.brief';

export const EMOTIONS: { key: EmotionKey; label: string; icon: string }[] = [
  { key: 'moved', label: 'שתתרגש', icon: 'heart' },
  { key: 'surprised', label: 'שתופתע', icon: 'star' },
  { key: 'pampered', label: 'שתתפנק', icon: 'spa' },
  { key: 'thrilled', label: 'שתתלהב', icon: 'sparkle' },
  { key: 'laugh', label: 'שתצחק', icon: 'smile' },
  { key: 'adventure', label: 'הרפתקה', icon: 'jeep' }
];

/** The brief being built across the wizard steps. Signals + sessionStorage so a refresh keeps progress. */
@Injectable({ providedIn: 'root' })
export class BriefStore {
  readonly draft = signal<BriefDraft>(this.restore());

  readonly canContinue = computed(() => !!this.draft().relationship && !!this.draft().occasion);
  readonly canSubmit = computed(() => this.canContinue() && !!this.draft().ageRange);

  /** "אשתי · 700 ₪ · יום הולדת" — the summary pill on the ideas screen. */
  readonly summary = computed(() => {
    const d = this.draft();
    return [d.recipientName ?? d.relationship?.label, d.budget ? `${d.budget} ₪` : null, d.occasion?.label]
      .filter(Boolean).join(' · ');
  });

  constructor() {
    effect(() => {
      const d = this.draft();
      try { sessionStorage.setItem(KEY, JSON.stringify(d)); } catch { /* ignore */ }
    });
  }

  patch(p: Partial<BriefDraft>) { this.draft.update(d => ({ ...d, ...p })); }

  reset() { this.draft.set({ ...EMPTY }); }

  toggleInterest(label: string) {
    this.draft.update(d => ({
      ...d,
      interests: d.interests.includes(label) ? d.interests.filter(i => i !== label) : [...d.interests, label]
    }));
  }

  addAvoid(term: string) {
    const t = term.trim();
    if (!t) return;
    this.draft.update(d => (d.avoid.includes(t) ? d : { ...d, avoid: [...d.avoid, t] }));
  }

  removeAvoid(term: string) { this.draft.update(d => ({ ...d, avoid: d.avoid.filter(a => a !== term) })); }

  toggleEmotion(key: EmotionKey) {
    this.draft.update(d => {
      if (d.emotions.includes(key)) return { ...d, emotions: d.emotions.filter(e => e !== key) };
      // Max two emotions (the server enforces the same rule): replace the oldest.
      const next = [...d.emotions, key];
      return { ...d, emotions: next.length > 2 ? next.slice(-2) : next };
    });
  }

  /** Pre-fill from a saved person ("או מישהו ששמרת"). */
  fromRecipient(r: RecipientDto, ageLabel: string | null) {
    this.patch({
      recipientId: r.id,
      recipientName: r.nickname,
      relationship: { key: r.relationshipKey, label: r.relationshipLabel },
      ageRange: { key: r.ageRangeKey, label: ageLabel ?? r.ageRangeKey },
      interests: [...r.interests],
      avoid: [...r.avoid],
      notes: r.notes ?? '',
      occasion: r.next ? { key: r.next.typeKey, label: r.next.label } : this.draft().occasion,
      eventDate: r.next?.date ?? this.draft().eventDate
    });
  }

  toRequest(): CreateSessionRequest {
    const d = this.draft();
    return {
      recipientId: d.recipientId,
      relationshipKey: d.relationship!.key,
      occasionKey: d.occasion!.key,
      eventDate: d.eventDate,
      ageRangeKey: d.ageRange!.key,
      interests: d.interests,
      avoid: d.avoid,
      notes: d.notes.trim() || null,
      budget: d.budget,
      emotions: d.emotions
    };
  }

  private restore(): BriefDraft {
    try {
      const raw = sessionStorage.getItem(KEY);
      return raw ? { ...EMPTY, ...(JSON.parse(raw) as BriefDraft) } : { ...EMPTY };
    } catch {
      return { ...EMPTY };
    }
  }
}
