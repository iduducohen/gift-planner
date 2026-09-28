// Mirrors server/src/GiftPlanner.Application/Contracts/Dtos.cs (camelCase JSON).

export interface TokenDto { token: string; userId: string; isAnonymous: boolean; displayName: string | null; }

export interface OptionDto { key: string; label: string; note?: string | null; }
export interface OptionGroupDto { title: string; options: OptionDto[]; }
export interface GroupedOptionsDto { common: OptionDto[]; groups: OptionGroupDto[]; }
export interface InterestCategoryDto { id: string; title: string; items: string[]; }
export interface InterestsDto { audienceLabel: string; recommended: string[]; categories: InterestCategoryDto[]; total: number; }

export type EmotionKey = 'moved' | 'surprised' | 'pampered' | 'thrilled' | 'laugh' | 'adventure';
export type KindKey = 'experience' | 'product' | 'personal' | 'pamper' | 'bundle';

export interface CreateSessionRequest {
  recipientId: string | null;
  relationshipKey: string;
  occasionKey: string;
  eventDate: string | null; // yyyy-MM-dd
  ageRangeKey: string;
  interests: string[];
  avoid: string[];
  notes: string | null;
  budget: number;
  emotions: EmotionKey[];
}

export interface QuestionOptionDto { key: string; label: string; hint: string; }
export interface QuestionDto { key: string; text: string; options: QuestionOptionDto[]; }
export interface SessionDto { id: string; status: string; question: QuestionDto | null; }

export interface IdeaDto {
  id: string;
  rank: number;
  isTop: boolean;
  title: string;
  kind: KindKey;
  kindLabel: string;
  why: string;
  emotionalValue: string | null;
  howTo: string | null;
  upgrade: string | null;
  imageUrl?: string | null;
  priceMin: number;
  priceMax: number;
  listPrice: number | null;
  discountPercent: number | null;
  priceSource: string;
  surprise: number;
  effort: number;
  iconKey: string;
  isAiWritten: boolean;
  saved: boolean;
}

export interface IdeaPageDto { items: IdeaDto[]; nextCursor: string | null; total: number; }
export interface IdeaSetDto { id: string; sessionId: string; refineLabel: string | null; total: number; top: IdeaDto[]; more: IdeaPageDto; }

export interface OfferDto {
  id: string; partnerName: string; partnerRegion: string; partnerPhone?: string | null; partnerImage?: string | null; partnerVerified: boolean;
  listPrice: number; discountPercent: number; finalPrice: number; priceSource: string; verifiedAt: string;
}
export interface IdeaDetailDto { idea: IdeaDto; offer: OfferDto | null; includes: string[]; catalogVerifiedAt: string | null; }
export interface RefineOptionDto { key: string; label: string; }

export interface NextOccasionDto { id: string; typeKey: string; label: string; date: string; daysLeft: number; }
export interface OccasionDto { id: string; typeKey: string; label: string; month: number; day: number; recurring: boolean; remindDaysBefore: number; }
export interface GiftHistoryDto { id: string; title: string; givenOn: string; reaction: string | null; }
export interface RecipientSummaryDto {
  id: string; nickname: string; relationshipKey: string; relationshipLabel: string; ageRangeKey: string; next: NextOccasionDto | null;
}
export interface RecipientDto extends RecipientSummaryDto {
  interests: string[]; avoid: string[]; notes: string | null; occasions: OccasionDto[]; history: GiftHistoryDto[];
}
export interface UpsertRecipientRequest {
  nickname: string; relationshipKey: string; ageRangeKey: string; interests: string[]; avoid: string[]; notes: string | null;
}

export interface SavedIdeaDto { idea: IdeaDto; recipientId: string | null; recipientNickname: string | null; savedAt: string; }

export interface PlanStepDto { time: string; title: string; detail: string; costMin: number; costMax: number; }
export interface PlanTaskDto { id: string; title: string; dueDate: string; done: boolean; }
export interface PlanDto {
  id: string; recommendationId: string; recipientId: string | null; title: string; eventDate: string; daysLeft: number;
  totalMin: number; totalMax: number; planB: string | null; steps: PlanStepDto[]; tasks: PlanTaskDto[];
}

export type PurchaseChannel = 'partner' | 'in_app';
export interface PurchaseResultDto {
  intentId: string; channel: string; status: string; redirectUrl: string | null; couponCode: string; price: number; planId: string;
}

export interface ProblemDetails { title?: string; status?: number; code?: string; }
