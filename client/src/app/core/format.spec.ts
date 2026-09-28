import { shekel, finalPrice, priceLabel, hasDeal, dealLabel, avatar, parseDate, toIso, shortDate, daysUntil, inDays, pronouns } from './format';

describe('Format utilities', () => {
  describe('shekel', () => {
    it('should format number as shekel currency', () => {
      const result = shekel(1000);
      expect(result).toContain('₪');
      expect(result).toContain('1');
    });

    it('should handle zero', () => {
      const result = shekel(0);
      expect(result).toContain('₪');
    });
  });

  describe('finalPrice', () => {
    it('should return discounted price when list price and discount exist', () => {
      const idea = { listPrice: 1000, discountPercent: 20 } as any;
      expect(finalPrice(idea)).toBe(800);
    });

    it('should return list price when no discount', () => {
      const idea = { listPrice: 500, discountPercent: 0 } as any;
      expect(finalPrice(idea)).toBe(500);
    });

    it('should return average of min and max when no list price', () => {
      const idea = { priceMin: 200, priceMax: 400 } as any;
      expect(finalPrice(idea)).toBe(300);
    });
  });

  describe('priceLabel', () => {
    it('should return "ללא עלות" for zero price', () => {
      const idea = { priceMax: 0 } as any;
      expect(priceLabel(idea)).toBe('ללא עלות');
    });

    it('should return formatted price for single value', () => {
      const idea = { priceMin: 500, priceMax: 500, listPrice: undefined } as any;
      const result = priceLabel(idea);
      expect(result).toContain('₪');
    });

    it('should return range when min differs from max', () => {
      const idea = { priceMin: 300, priceMax: 600, listPrice: undefined } as any;
      const result = priceLabel(idea);
      expect(result).toContain('–');
    });
  });

  describe('hasDeal', () => {
    it('should return true when discount exists and is positive', () => {
      const idea = { discountPercent: 20 } as any;
      expect(hasDeal(idea)).toBe(true);
    });

    it('should return false when no discount', () => {
      const idea = { discountPercent: 0 } as any;
      expect(hasDeal(idea)).toBe(false);
    });

    it('should return false when discount is undefined', () => {
      const idea = { discountPercent: undefined } as any;
      expect(hasDeal(idea)).toBe(false);
    });
  });

  describe('dealLabel', () => {
    it('should format discount as percentage label', () => {
      const idea = { discountPercent: 25 } as any;
      expect(dealLabel(idea)).toBe('‎-25%');
    });
  });

  describe('avatar', () => {
    it('should return consistent color pair for same name', () => {
      const result1 = avatar('John');
      const result2 = avatar('John');
      expect(result1).toEqual(result2);
    });

    it('should return array with two colors', () => {
      const result = avatar('Test Name');
      expect(Array.isArray(result)).toBe(true);
      expect(result.length).toBe(2);
      expect(result[0]).toMatch(/^#/);
      expect(result[1]).toMatch(/^#/);
    });

    it('should return different colors for different names', () => {
      const result1 = avatar('Alice');
      const result2 = avatar('Bob');
      expect(result1).not.toEqual(result2);
    });
  });

  describe('date utilities', () => {
    it('parseDate should parse ISO date string', () => {
      const date = parseDate('2026-09-28');
      expect(date.getFullYear()).toBe(2026);
      expect(date.getMonth()).toBe(8); // 0-indexed
      expect(date.getDate()).toBe(28);
    });

    it('toIso should convert Date to ISO string', () => {
      const date = new Date(2026, 8, 28);
      expect(toIso(date)).toBe('2026-09-28');
    });

    it('shortDate should format date in Hebrew', () => {
      const result = shortDate('2026-09-28');
      expect(result).toContain('.');
      expect(result).toContain(',');
    });

    it('daysUntil should calculate days from today', () => {
      const today = new Date();
      const iso = toIso(today);
      expect(daysUntil(iso)).toBe(0);
    });

    it('inDays should format days description', () => {
      expect(inDays(0)).toBe('היום');
      expect(inDays(1)).toBe('מחר');
      expect(inDays(-1)).toBe('עבר');
      expect(inDays(7)).toBe('בעוד 7 ימים');
    });
  });

  describe('pronouns', () => {
    it('should return female pronouns for female relationships', () => {
      const result = pronouns('partner_f');
      expect(result.likes).toBe('אוהבת');
      expect(result.has).toBe('יש לה');
    });

    it('should return male pronouns for male relationships', () => {
      const result = pronouns('partner_m');
      expect(result.likes).toBe('אוהב');
      expect(result.has).toBe('יש לו');
    });

    it('should return neutral pronouns for unknown relationship', () => {
      const result = pronouns(null);
      expect(result.likes).toBe('אוהב/ת');
      expect(result.toHer).toBe('לו/ה');
    });
  });
});
