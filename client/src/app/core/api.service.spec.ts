import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ApiService, API_BASE } from './api.service';
import { EmotionKey } from './models';

describe('ApiService', () => {
  let service: ApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ApiService]
    });
    service = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('taxonomy endpoints', () => {
    it('should fetch relationships', () => {
      service.relationships$.subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/taxonomy/relationships`);
      expect(req.request.method).toBe('GET');
      req.flush({ groups: [] });
    });

    it('should fetch occasions', () => {
      service.occasions$.subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/taxonomy/occasions`);
      expect(req.request.method).toBe('GET');
      req.flush({ groups: [] });
    });

    it('should fetch age ranges', () => {
      service.ageRanges$.subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/taxonomy/age-ranges`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });
  });

  describe('auth endpoints', () => {
    it('should post anonymous auth', () => {
      service.anonymous().subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/auth/anonymous`);
      expect(req.request.method).toBe('POST');
      req.flush({ token: 'test-token' });
    });

    it('should post sign-in request', () => {
      service.signIn('google', 'test-id-token').subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/auth/sign-in`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ provider: 'google', idToken: 'test-id-token' });
      req.flush({ token: 'test-token' });
    });
  });

  describe('session endpoints', () => {
    it('should create a session', () => {
      const createReq = {
        recipientId: null,
        relationshipKey: 'partner_f',
        occasionKey: 'birthday',
        eventDate: null,
        ageRangeKey: '35-44',
        interests: ['photography'],
        avoid: [],
        notes: null,
        budget: 500,
        emotions: ['moved'] as EmotionKey[]
      };
      service.createSession(createReq).subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/gift-sessions`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createReq);
      req.flush({ id: 'session-1', status: 'pending', question: null });
    });

    it('should clarify a session', () => {
      const sessionId = 'session-1';
      service.clarify(sessionId, 'answer-key').subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/gift-sessions/${sessionId}/clarifications`);
      expect(req.request.method).toBe('POST');
      req.flush({ id: sessionId });
    });
  });

  describe('idea endpoints', () => {
    it('should fetch an idea detail', () => {
      const ideaId = 'idea-1';
      service.idea(ideaId).subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/ideas/${ideaId}`);
      expect(req.request.method).toBe('GET');
      req.flush({ id: ideaId, title: 'Test Gift' });
    });

    it('should save an idea', () => {
      const ideaId = 'idea-1';
      const recipientId = 'recipient-1';
      service.save(ideaId, recipientId).subscribe(() => {
        expect(true).toBe(true);
      });

      const req = httpMock.expectOne(`${API_BASE}/ideas/${ideaId}/saved`);
      expect(req.request.method).toBe('PUT');
      req.flush(null);
    });

    it('should unsave an idea', () => {
      const ideaId = 'idea-1';
      service.unsave(ideaId).subscribe(() => {
        expect(true).toBe(true);
      });

      const req = httpMock.expectOne(`${API_BASE}/ideas/${ideaId}/saved`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('recipient endpoints', () => {
    it('should fetch all recipients', () => {
      service.recipients().subscribe(result => {
        expect(Array.isArray(result)).toBe(true);
      });

      const req = httpMock.expectOne(`${API_BASE}/recipients`);
      expect(req.request.method).toBe('GET');
      req.flush([]);
    });

    it('should fetch a single recipient', () => {
      const recipientId = 'recipient-1';
      service.recipient(recipientId).subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/recipients/${recipientId}`);
      expect(req.request.method).toBe('GET');
      req.flush({ id: recipientId, name: 'Test Person' });
    });

    it('should create a recipient', () => {
      const createReq = {
        nickname: 'John',
        relationshipKey: 'partner_m',
        ageRangeKey: '25-34',
        interests: [],
        avoid: [],
        notes: null
      };
      service.createRecipient(createReq).subscribe(result => {
        expect(result).toBeTruthy();
      });

      const req = httpMock.expectOne(`${API_BASE}/recipients`);
      expect(req.request.method).toBe('POST');
      req.flush({
        id: 'recipient-1',
        nickname: 'John',
        relationshipKey: 'partner_m',
        relationshipLabel: 'Partner',
        ageRangeKey: '25-34',
        interests: [],
        avoid: [],
        notes: null,
        occasions: [],
        history: [],
        next: null
      });
    });

    it('should delete a recipient', () => {
      const recipientId = 'recipient-1';
      service.deleteRecipient(recipientId).subscribe(() => {
        expect(true).toBe(true);
      });

      const req = httpMock.expectOne(`${API_BASE}/recipients/${recipientId}`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
