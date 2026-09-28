import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import {
  CreateSessionRequest, GroupedOptionsDto, IdeaDetailDto, IdeaPageDto, IdeaSetDto, InterestsDto, OptionGroupDto,
  PlanDto, PurchaseChannel, PurchaseResultDto, RecipientDto, RecipientSummaryDto, RefineOptionDto, SavedIdeaDto,
  SessionDto, TokenDto, UpsertRecipientRequest
} from './models';

export const API_BASE = '/api/v1';

/** Thin typed client over the REST API. No state here — stores and pages own state. */
@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  // Taxonomy never changes during a session: cache the observables.
  readonly relationships$ = this.http.get<GroupedOptionsDto>(`${API_BASE}/taxonomy/relationships`).pipe(shareReplay(1));
  readonly occasions$ = this.http.get<GroupedOptionsDto>(`${API_BASE}/taxonomy/occasions`).pipe(shareReplay(1));
  readonly ageRanges$ = this.http.get<OptionGroupDto[]>(`${API_BASE}/taxonomy/age-ranges`).pipe(shareReplay(1));
  readonly refineOptions$ = this.http.get<RefineOptionDto[]>(`${API_BASE}/refine-options`).pipe(shareReplay(1));

  interests(relationship: string | null, ageRange: string | null): Observable<InterestsDto> {
    let params = new HttpParams();
    if (relationship) params = params.set('relationship', relationship);
    if (ageRange) params = params.set('ageRange', ageRange);
    return this.http.get<InterestsDto>(`${API_BASE}/taxonomy/interests`, { params });
  }

  // ---- auth ----
  anonymous() { return this.http.post<TokenDto>(`${API_BASE}/auth/anonymous`, {}); }
  signIn(provider: string, idToken: string) { return this.http.post<TokenDto>(`${API_BASE}/auth/sign-in`, { provider, idToken }); }

  // ---- sessions & ideas ----
  createSession(req: CreateSessionRequest) { return this.http.post<SessionDto>(`${API_BASE}/gift-sessions`, req); }
  clarify(sessionId: string, answerKey: string | null) {
    return this.http.post<SessionDto>(`${API_BASE}/gift-sessions/${sessionId}/clarifications`, { answerKey });
  }
  saveSessionRecipient(sessionId: string, nickname: string) {
    return this.http.post<RecipientDto>(`${API_BASE}/gift-sessions/${sessionId}/recipient`, { nickname });
  }
  generate(sessionId: string, opts: { parentSetId?: string; refinePreset?: string; refineText?: string } = {}) {
    return this.http.post<IdeaSetDto>(`${API_BASE}/gift-sessions/${sessionId}/idea-sets`, {
      parentSetId: opts.parentSetId ?? null, refinePreset: opts.refinePreset ?? null, refineText: opts.refineText ?? null
    });
  }
  ideaSet(setId: string) { return this.http.get<IdeaSetDto>(`${API_BASE}/idea-sets/${setId}`); }
  ideasPage(setId: string, cursor: string | null, limit: number, filter: string) {
    let params = new HttpParams().set('limit', limit).set('filter', filter);
    if (cursor) params = params.set('cursor', cursor);
    return this.http.get<IdeaPageDto>(`${API_BASE}/idea-sets/${setId}/ideas`, { params });
  }
  idea(id: string) { return this.http.get<IdeaDetailDto>(`${API_BASE}/ideas/${id}`); }
  save(id: string, recipientId: string | null) { return this.http.put<void>(`${API_BASE}/ideas/${id}/saved`, { recipientId }); }
  unsave(id: string) { return this.http.delete<void>(`${API_BASE}/ideas/${id}/saved`); }
  createPlan(id: string, eventDate: string | null) { return this.http.post<PlanDto>(`${API_BASE}/ideas/${id}/plan`, { eventDate }); }
  purchase(id: string, forDate: string | null, channel: PurchaseChannel) {
    return this.http.post<PurchaseResultDto>(`${API_BASE}/ideas/${id}/purchase-intents`, { forDate, channel });
  }

  // ---- people ----
  recipients() { return this.http.get<RecipientSummaryDto[]>(`${API_BASE}/recipients`); }
  recipient(id: string) { return this.http.get<RecipientDto>(`${API_BASE}/recipients/${id}`); }
  createRecipient(req: UpsertRecipientRequest) { return this.http.post<RecipientDto>(`${API_BASE}/recipients`, req); }
  updateRecipient(id: string, req: UpsertRecipientRequest) { return this.http.put<RecipientDto>(`${API_BASE}/recipients/${id}`, req); }
  deleteRecipient(id: string) { return this.http.delete<void>(`${API_BASE}/recipients/${id}`); }
  addOccasion(id: string, typeKey: string, month: number, day: number) {
    return this.http.post<RecipientDto>(`${API_BASE}/recipients/${id}/occasions`, { typeKey, month, day, recurring: true, remindDaysBefore: 10 });
  }
  removeOccasion(id: string, occasionId: string) { return this.http.delete<RecipientDto>(`${API_BASE}/recipients/${id}/occasions/${occasionId}`); }
  recordGift(id: string, title: string, givenOn: string, reaction: string | null) {
    return this.http.post<RecipientDto>(`${API_BASE}/recipients/${id}/gift-history`, { title, givenOn, reaction });
  }

  // ---- saved & plans ----
  saved(recipientId?: string | null) {
    const params = recipientId ? new HttpParams().set('recipientId', recipientId) : undefined;
    return this.http.get<SavedIdeaDto[]>(`${API_BASE}/saved`, { params });
  }
  plans() { return this.http.get<PlanDto[]>(`${API_BASE}/plans`); }
  plan(id: string) { return this.http.get<PlanDto>(`${API_BASE}/plans/${id}`); }
  setTask(planId: string, taskId: string, done: boolean) {
    return this.http.patch<PlanDto>(`${API_BASE}/plans/${planId}/tasks/${taskId}`, { done });
  }

  // ---- account ----
  exportData() { return this.http.get<unknown>(`${API_BASE}/me/export`); }
  deleteAccount() { return this.http.delete<void>(`${API_BASE}/me`); }
}
