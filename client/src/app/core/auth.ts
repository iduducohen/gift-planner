import { HttpBackend, HttpClient, HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, finalize, firstValueFrom, from, shareReplay, switchMap, tap, throwError } from 'rxjs';
import { API_BASE } from './api.service';
import { TokenDto } from './models';

const STORAGE_KEY = 'gp.token';

function readStored(): TokenDto | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as TokenDto) : null;
  } catch {
    return null;
  }
}

/**
 * Anonymous-first auth: the first API call silently gets an anonymous token, so the user never
 * hits a sign-up wall. Signing in later merges the anonymous data on the server.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  // HttpBackend bypasses interceptors, so fetching a token can't recurse into the interceptor.
  private readonly raw = new HttpClient(inject(HttpBackend));
  private readonly state = signal<TokenDto | null>(readStored());
  private pending: Observable<TokenDto> | null = null;

  readonly user = this.state.asReadonly();
  readonly isSignedIn = computed(() => !!this.state() && !this.state()!.isAnonymous);
  readonly displayName = computed(() => this.state()?.displayName ?? null);

  get token(): string | null { return this.state()?.token ?? null; }

  /** Returns a valid token, creating an anonymous user once if needed (concurrent callers share the request). */
  ensureToken(): Observable<TokenDto> {
    const current = this.state();
    if (current) return from([current]);
    if (!this.pending) {
      this.pending = this.raw.post<TokenDto>(`${API_BASE}/auth/anonymous`, {}).pipe(
        tap(t => this.set(t)),
        finalize(() => (this.pending = null)),
        shareReplay(1)
      );
    }
    return this.pending;
  }

  async signIn(provider: string, idToken: string): Promise<TokenDto> {
    const headers = this.token ? { Authorization: `Bearer ${this.token}` } : undefined;
    const t = await firstValueFrom(this.raw.post<TokenDto>(`${API_BASE}/auth/sign-in`, { provider, idToken }, { headers }));
    this.set(t);
    return t;
  }

  signOut() { this.set(null); }

  /** Called when the server rejects the token (expired / deleted account). */
  reset() { this.set(null); }

  private set(t: TokenDto | null) {
    this.state.set(t);
    try {
      if (t) localStorage.setItem(STORAGE_KEY, JSON.stringify(t));
      else localStorage.removeItem(STORAGE_KEY);
    } catch { /* storage unavailable: token lives for this session only */ }
  }
}

const isPublic = (req: HttpRequest<unknown>) => req.url.includes('/taxonomy/') || req.url.includes('/auth/');

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith(API_BASE) || isPublic(req)) return next(req);
  const auth = inject(AuthService);
  const withToken = (token: string) => req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

  return auth.ensureToken().pipe(
    switchMap(t => next(withToken(t.token))),
    catchError((err: unknown) => {
      // Token expired or user deleted: get a fresh anonymous token once and retry.
      if (err instanceof HttpErrorResponse && err.status === 401) {
        auth.reset();
        return auth.ensureToken().pipe(switchMap(t => next(withToken(t.token))));
      }
      return throwError(() => err);
    })
  );
};
