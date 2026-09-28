import { IdeaDto, KindKey } from './models';

const nf = new Intl.NumberFormat('he-IL');

export const shekel = (n: number) => `${nf.format(n)} ₪`;

/** Final price of an idea: the partner price after discount when there is one, else the typical price. */
export function finalPrice(i: IdeaDto): number {
  if (i.listPrice && i.discountPercent) return Math.round(i.listPrice * (100 - i.discountPercent) / 100);
  return i.listPrice ?? Math.round((i.priceMin + i.priceMax) / 2);
}

export function priceLabel(i: IdeaDto): string {
  if (i.priceMax === 0) return 'ללא עלות';
  if (i.listPrice) return shekel(finalPrice(i));
  return i.priceMin === i.priceMax ? shekel(i.priceMin) : `${nf.format(i.priceMin)}–${shekel(i.priceMax)}`;
}

export const hasDeal = (i: IdeaDto) => !!i.discountPercent && i.discountPercent > 0;

export const dealLabel = (i: IdeaDto) => `‎-${i.discountPercent}%`;

/** Soft tint + ink per idea kind, from the design. */
export const KIND_TINT: Record<KindKey, [string, string]> = {
  experience: ['#F3E7D9', '#8A6A4A'],
  bundle: ['#F3E7D9', '#8A6A4A'],
  pamper: ['#F7E5EA', '#9A4A5E'],
  product: ['#E4ECE8', '#4E6B5E'],
  personal: ['#ECE6F2', '#65588A']
};

export const tint = (k: KindKey) => KIND_TINT[k] ?? KIND_TINT.experience;

/** Avatar colours for people, picked by a stable hash of the name. */
const AVATARS: [string, string][] = [['#FBEAEE', '#A3223F'], ['#E4ECE8', '#3F5E50'], ['#ECE6F2', '#5B4E80'], ['#F3E7D9', '#8A6A4A']];
export function avatar(name: string): [string, string] {
  let h = 0;
  for (const ch of name) h = (h * 31 + ch.charCodeAt(0)) >>> 0;
  return AVATARS[h % AVATARS.length];
}

const days = ['ראשון', 'שני', 'שלישי', 'רביעי', 'חמישי', 'שישי', 'שבת'];

export function parseDate(iso: string): Date {
  const [y, m, d] = iso.split('-').map(Number);
  return new Date(y, m - 1, d);
}

export function toIso(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

/** "שבת, 14.3" */
export function shortDate(iso: string): string {
  const d = parseDate(iso);
  return `${days[d.getDay()]}, ${d.getDate()}.${d.getMonth() + 1}`;
}

export function daysUntil(iso: string): number {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return Math.round((parseDate(iso).getTime() - today.getTime()) / 86_400_000);
}

export function inDays(n: number): string {
  if (n === 0) return 'היום';
  if (n === 1) return 'מחר';
  if (n < 0) return 'עבר';
  return `בעוד ${n} ימים`;
}

const FEMALE = new Set(['partner_f', 'fiancee', 'mom', 'daughter', 'sister', 'grandma', 'granddaughter', 'mother_in_law',
  'sister_in_law', 'aunt', 'niece', 'friend_f', 'new_date_f']);
const MALE = new Set(['partner_m', 'fiance', 'dad', 'son', 'brother', 'grandpa', 'grandson', 'father_in_law', 'brother_in_law',
  'uncle', 'nephew', 'friend_m']);

export interface Pronouns { about: string; likes: string; has: string; her: string; forHer: string; toHer: string; }

/** Hebrew is gendered; copy follows the chosen relationship, with a neutral slash form otherwise. */
export function pronouns(relationshipKey: string | null | undefined): Pronouns {
  if (relationshipKey && FEMALE.has(relationshipKey)) return { about: 'עליה', likes: 'אוהבת', has: 'יש לה', her: 'היא', forHer: 'בשבילה', toHer: 'לה' };
  if (relationshipKey && MALE.has(relationshipKey)) return { about: 'עליו', likes: 'אוהב', has: 'יש לו', her: 'הוא', forHer: 'בשבילו', toHer: 'לו' };
  return { about: 'עליו/ה', likes: 'אוהב/ת', has: 'יש לו/ה', her: 'הוא/היא', forHer: 'בשבילו/ה', toHer: 'לו/ה' };
}
