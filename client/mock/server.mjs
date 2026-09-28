// Tiny in-memory mock of the REST API — for UI work and screenshots without .NET/Postgres.
// Run: npm run mock   (serves the API on :5080; `npm start` proxies /api to it)
import { createServer } from 'node:http';
import { randomUUID } from 'node:crypto';

const opt = (key, label) => ({ key, label });
const taxonomy = {
  relationships: {
    common: [opt('partner_f', 'בת זוג / אשתי'), opt('partner_m', 'בן זוג / בעלי'), opt('mom', 'אמא'), opt('dad', 'אבא'), opt('friend_f', 'חברה טובה'), opt('colleague', 'עמית/ה לעבודה')],
    groups: [
      { title: 'משפחה', options: [opt('daughter', 'בת'), opt('son', 'בן'), opt('sister', 'אחות'), opt('brother', 'אח'), opt('grandma', 'סבתא'), opt('grandpa', 'סבא')] },
      { title: 'חברים', options: [opt('friend_m', 'חבר טוב'), opt('couple_friends', 'זוג חברים')] },
      { title: 'עבודה', options: [opt('manager', 'מנהל/ת'), opt('team', 'צוות שלם')] }
    ]
  },
  occasions: {
    common: [opt('birthday', 'יום הולדת'), opt('anniversary', 'יום נישואין'), opt('just_because', 'סתם כי בא לי'), opt('thank_you', 'תודה')],
    groups: [{ title: 'חגים', options: [opt('rosh_hashana', 'ראש השנה'), opt('hanukkah', 'חנוכה'), opt('valentines', 'ולנטיין')] },
             { title: 'אבני דרך', options: [opt('milestone_birthday', 'יום הולדת עגול'), opt('new_home', 'חנוכת בית'), opt('birth', 'לידה')] }]
  },
  ages: [
    { title: 'ילדים', options: [opt('3-5', '3–5'), opt('6-8', '6–8'), opt('9-12', '9–12')] },
    { title: 'נוער', options: [opt('13-15', '13–15'), opt('16-17', '16–17')] },
    { title: 'מבוגרים', options: [opt('25-34', '25–34'), opt('35-44', '35–44'), opt('45-54', '45–54')] },
    { title: 'גיל הזהב', options: [opt('65-74', '65–74'), opt('75+', '75+')] }
  ]
};
const cats = [
  { id: 'nature', title: 'טבע וטיולים', items: ['טיולים בארץ', 'שקיעות', 'ים וחופים', 'מדבר', 'טבע וציפורים'] },
  { id: 'art', title: 'יצירה ואמנות', items: ['צילום', 'קרמיקה', 'ציור', 'DIY', 'כתיבה'] },
  { id: 'food', title: 'אוכל ושתייה', items: ['מסעדות שף', 'יין', 'קפה', 'שוקולד', 'בישול', 'אפייה'] },
  { id: 'wellness', title: 'פינוק ובריאות', items: ['ספא', 'עיסויים', 'יוגה', 'פילאטיס', 'נרות וריחות'] },
  ...Array.from({ length: 12 }, (_, i) => ({ id: `c${i}`, title: `קטגוריה ${i + 1}`, items: Array.from({ length: 10 }, (_, j) => `תחום ${i + 1}.${j + 1}`) }))
];

const catalog = [
  ['פיקניק שקיעה אישי', 'experience', 'חוויה', 'sunset', 595, 700, 15, 'טבע, צילום ורומנטיקה ברגע אחד.'],
  ['יום הולדת בשלושה רגעים', 'bundle', 'חבילה · 3 עסקים', 'gift', 675, 750, 10, 'פרחים בבוקר, מתנה בצהריים, ארוחה בערב.'],
  ['אלבום מהטיולים שלכם', 'personal', 'מתנה אישית', 'frame', 288, 360, 20, 'התמונות שהיא צילמה, סוף סוף על נייר.'],
  ['בוקר ספא זוגי + פרחים בחדר', 'pamper', 'פינוק', 'spa', 640, 640, 0, 'הפינוק שהיא אוהבת, עם תוספת שלך.'],
  ['סדנת צילום שקיעה עם צלם', 'experience', 'משהו בלתי צפוי', 'camera', 450, 530, 15, 'משהו שלא הייתה מזמינה לעצמה.'],
  ['סדנת קרמיקה זוגית', 'experience', 'חוויה', 'cup', 423, 470, 10], ['ארוחת טעימות אצל שף', 'experience', 'חוויה', 'plate', 638, 750, 15],
  ['זר עונתי + שוקולד בוטיק', 'product', 'מתנה', 'flower', 243, 270, 10], ['הדפסת צילום אהוב על קנבס', 'personal', 'אישי', 'frame', 208, 260, 20],
  ['סיור טעימות ביקב', 'experience', 'חוויה', 'wine', 357, 420, 15], ['קופסת ספא לבית', 'pamper', 'פינוק', 'spa', 280, 280, 0],
  ['ערב סטנדאפ + ארוחה', 'experience', 'חוויה', 'mic', 480, 480, 0], ['עגילים בעיצוב אישי', 'personal', 'אישי', 'ring', 387, 430, 10],
  ['לילה במלון בוטיק בעיר', 'experience', 'חוויה', 'bed', 690, 690, 0], ['שייט שקיעה ביפו', 'experience', 'חוויה', 'boat', 595, 700, 15],
  ['כרטיסים להופעה', 'experience', 'חוויה', 'ticket', 500, 500, 0], ['מנוי לשיעורי יוגה', 'pamper', 'פינוק', 'heart', 450, 450, 0],
  ['ערכת נרות ובישום לבית', 'product', 'מתנה', 'candle', 230, 230, 0], ['אלבום ״365 סיבות״ בכתב ידך', 'personal', 'אישי', 'book', 150, 150, 0],
  ['טיול ג׳יפים במדבר', 'experience', 'חוויה', 'jeep', 621, 690, 10], ['תיק צילום מעוצב', 'product', 'מתנה', 'bag', 380, 380, 0],
  ['סרטון ברכות מחברים', 'personal', 'אישי', 'mic', 0, 0, 0], ['מצלמת פילם מיידית', 'product', 'מתנה', 'camera', 420, 420, 0],
  ['סדנת בישול איטלקי זוגית', 'experience', 'חוויה', 'plate', 520, 520, 0], ['ספר מתכונים משפחתי', 'personal', 'אישי', 'book', 260, 260, 0],
  ['ארוחת בוקר עד הבית', 'product', 'מתנה', 'coffee', 190, 190, 0], ['עציץ מעוצב לשולחן', 'product', 'מתנה', 'plant', 140, 140, 0],
  ['חדר בריחה לחבורה', 'experience', 'חוויה', 'puzzle', 350, 350, 0], ['ספר עם הקדשה אישית', 'personal', 'אישי', 'book', 90, 90, 0]
];

const db = { users: new Map(), sets: new Map(), ideas: new Map(), saved: new Map(), recipients: new Map(), plans: new Map() };

function makeSet(sessionId, label) {
  const id = randomUUID();
  const items = catalog.map((c, i) => {
    const [title, kind, kindLabel, iconKey, price, list, disc, why] = c;
    const idea = {
      id: randomUUID(), rank: i + 1, isTop: i < 5, title, kind, kindLabel, why: why ?? 'רעיון שמתאים למה שסיפרת.',
      emotionalValue: i < 5 ? 'רגע שתזכרו הרבה אחרי שהמתנה נגמרת.' : null, howTo: i < 5 ? 'תגיד רק מתי להיות מוכנה. את השאר תשאיר להפתעה.' : null,
      upgrade: i === 0 ? 'צלם ל-30 דקות, כדי שהפעם היא תהיה בתמונה' : null, priceMin: price, priceMax: price,
      listPrice: disc ? list : null, discountPercent: disc || null, priceSource: disc ? 'provider_listed' : 'catalog_estimate',
      surprise: 3 + (i % 3), effort: 2, iconKey, isAiWritten: i < 5, saved: false, _setId: id
    };
    db.ideas.set(idea.id, idea);
    return idea;
  });
  const set = { id, sessionId, refineLabel: label ?? null, items };
  db.sets.set(id, set);
  return set;
}
const clean = i => { const { _setId, ...rest } = i; return { ...rest, saved: db.saved.has(i.id) }; };
function page(set, cursor, limit, filter) {
  const rest = set.items.filter(i => !i.isTop && (filter === 'all' || !filter || (filter === 'deal' ? i.discountPercent : i.kind === filter)));
  const off = Number(cursor ?? 0);
  const items = rest.slice(off, off + limit).map(clean);
  return { items, nextCursor: off + items.length < rest.length ? String(off + items.length) : null, total: rest.length };
}
const setDto = s => ({ id: s.id, sessionId: s.sessionId, refineLabel: s.refineLabel, total: s.items.length, top: s.items.filter(i => i.isTop).map(clean), more: page(s, 0, 8, 'all') });
const iso = d => d.toISOString().slice(0, 10);
function plan(idea, eventDate) {
  const date = eventDate ?? iso(new Date(Date.now() + 12 * 864e5));
  const days = Math.round((new Date(date) - new Date(iso(new Date()))) / 864e5);
  const p = {
    id: randomUUID(), recommendationId: idea.id, recipientId: null, title: `תוכנית: ${idea.title}`, eventDate: date, daysLeft: days,
    totalMin: idea.priceMin + 150, totalMax: idea.priceMax + 150, planB: 'אם יורד גשם: אותו פיקניק בסלון, עם אורות ומוזיקה.',
    steps: [
      { time: '10:00', title: 'פרחים מגיעים הביתה', detail: 'עם פתק קצר: ״תשמרי את הערב פנוי״', costMin: 150, costMax: 150 },
      { time: '14:00', title: 'הודעת הפתעה', detail: '״תתלבשי נוח ויפה. אני אוסף אותך ב-17:30״', costMin: 0, costMax: 0 },
      { time: '18:00', title: idea.title, detail: 'הכול מוכן כשאתם מגיעים', costMin: idea.priceMin, costMax: idea.priceMax }
    ],
    tasks: [
      { id: randomUUID(), title: 'להזמין אצל העסק', dueDate: iso(new Date()), done: false },
      { id: randomUUID(), title: 'להזמין פרחים', dueDate: iso(new Date(Date.now() + 5 * 864e5)), done: false },
      { id: randomUUID(), title: 'לכתוב פתק', dueDate: iso(new Date(Date.now() + 10 * 864e5)), done: false }
    ]
  };
  db.plans.set(p.id, p);
  return p;
}

const recipient = r => ({ ...r, next: r.occasions[0] ? { id: r.occasions[0].id, typeKey: r.occasions[0].typeKey, label: r.occasions[0].label, date: iso(new Date(Date.now() + 12 * 864e5)), daysLeft: 12 } : null });
const seedPerson = (nickname, rel, relLabel, age, interests) => {
  const r = { id: randomUUID(), nickname, relationshipKey: rel, relationshipLabel: relLabel, ageRangeKey: age, interests, avoid: [], notes: null,
    occasions: [{ id: randomUUID(), typeKey: 'birthday', label: 'יום הולדת', month: 3, day: 14, recurring: true, remindDaysBefore: 10 }], history: [] };
  db.recipients.set(r.id, r);
};
seedPerson('אשתי', 'partner_f', 'בת זוג / אשתי', '35-44', ['צילום', 'שקיעות', 'יין']);
seedPerson('אמא', 'mom', 'אמא', '65-74', ['בישול', 'ספא']);

const routes = [
  ['POST', /^\/api\/v1\/auth\/anonymous$/, () => ({ token: 'mock.' + randomUUID(), userId: randomUUID(), isAnonymous: true, displayName: null })],
  ['POST', /^\/api\/v1\/auth\/sign-in$/, () => ({ token: 'mock.' + randomUUID(), userId: randomUUID(), isAnonymous: false, displayName: 'דודו' })],
  ['GET', /^\/api\/v1\/taxonomy\/relationships$/, () => taxonomy.relationships],
  ['GET', /^\/api\/v1\/taxonomy\/occasions$/, () => taxonomy.occasions],
  ['GET', /^\/api\/v1\/taxonomy\/age-ranges$/, () => taxonomy.ages],
  ['GET', /^\/api\/v1\/taxonomy\/interests$/, () => ({ audienceLabel: 'מבוגרים', recommended: ['צילום', 'שקיעות', 'ספא', 'יין', 'טיולים בארץ'], categories: cats, total: cats.reduce((n, c) => n + c.items.length, 0) })],
  ['GET', /^\/api\/v1\/refine-options$/, () => [['cheaper', 'יותר זול'], ['exciting', 'יותר מרגש'], ['surprise', 'יותר מפתיע'], ['experiences', 'רק חוויות'], ['products', 'רק מוצרים'], ['personal', 'יותר אישי']].map(([key, label]) => ({ key, label }))],
  ['POST', /^\/api\/v1\/gift-sessions$/, (m, b) => ({ id: randomUUID(), status: b.interests.length < 2 && !b.emotions.length ? 'awaiting_clarification' : 'ready',
    question: b.interests.length < 2 && !b.emotions.length ? { key: 'direction', text: 'מה ישמח יותר?', options: [{ key: 'romantic', label: 'חוויה בשניים', hint: 'ערב, יציאה, זמן יחד' }, { key: 'pamper', label: 'פינוק רק בשבילה', hint: 'זמן לעצמה, בלי להתאמץ' }, { key: 'unexpected', label: 'משהו שלא מצפים לו בכלל', hint: 'הפתעה אמיתית, מחוץ לקופסה' }] } : null })],
  ['POST', /^\/api\/v1\/gift-sessions\/([^/]+)\/clarifications$/, m => ({ id: m[1], status: 'ready', question: null })],
  ['POST', /^\/api\/v1\/gift-sessions\/([^/]+)\/idea-sets$/, (m, b) => setDto(makeSet(m[1], b.refinePreset ? { cheaper: 'יותר זול', surprise: 'יותר מפתיע' }[b.refinePreset] ?? b.refinePreset : b.refineText))],
  ['GET', /^\/api\/v1\/idea-sets\/([^/]+)$/, m => (db.sets.has(m[1]) ? setDto(db.sets.get(m[1])) : 404)],
  ['GET', /^\/api\/v1\/idea-sets\/([^/]+)\/ideas$/, (m, b, q) => page(db.sets.get(m[1]), q.get('cursor'), Number(q.get('limit') ?? 8), q.get('filter'))],
  ['GET', /^\/api\/v1\/ideas\/([^/]+)$/, m => { const i = db.ideas.get(m[1]); if (!i) return 404;
    return { idea: clean(i), offer: i.discountPercent ? { id: randomUUID(), partnerName: 'פיקניקים מעוצבים (דמו)', partnerRegion: 'מרכז', partnerVerified: true, listPrice: i.listPrice, discountPercent: i.discountPercent, finalPrice: i.priceMin, priceSource: 'provider_listed', verifiedAt: iso(new Date()) } : null,
      includes: ['מקום עם נוף לשקיעה', 'עיצוב, שמיכות ותאורה', 'מגש אירוח ויין', 'זר פרחים קטן'], catalogVerifiedAt: iso(new Date()) }; }],
  ['PUT', /^\/api\/v1\/ideas\/([^/]+)\/saved$/, (m, b) => { db.saved.set(m[1], { recipientId: b?.recipientId ?? null, at: new Date().toISOString() }); return null; }],
  ['DELETE', /^\/api\/v1\/ideas\/([^/]+)\/saved$/, m => { db.saved.delete(m[1]); return null; }],
  ['POST', /^\/api\/v1\/ideas\/([^/]+)\/plan$/, (m, b) => plan(db.ideas.get(m[1]), b?.eventDate)],
  ['POST', /^\/api\/v1\/ideas\/([^/]+)\/purchase-intents$/, (m, b) => { const i = db.ideas.get(m[1]); const p = plan(i, b.forDate);
    return { intentId: randomUUID(), channel: b.channel, status: 'created', redirectUrl: b.channel === 'partner' ? 'https://example.com/book?coupon=GIFT15' : null, couponCode: 'GIFT' + String(i.discountPercent ?? 0).padStart(2, '0'), price: i.priceMin, planId: p.id }; }],
  ['GET', /^\/api\/v1\/recipients$/, () => [...db.recipients.values()].map(r => { const { interests, avoid, notes, occasions, history, ...s } = recipient(r); return s; })],
  ['GET', /^\/api\/v1\/recipients\/([^/]+)$/, m => (db.recipients.has(m[1]) ? recipient(db.recipients.get(m[1])) : 404)],
  ['POST', /^\/api\/v1\/recipients$/, (m, b) => { const r = { id: randomUUID(), ...b, relationshipLabel: b.relationshipKey, occasions: [], history: [] }; db.recipients.set(r.id, r); return recipient(r); }],
  ['PUT', /^\/api\/v1\/recipients\/([^/]+)$/, (m, b) => { const r = { ...db.recipients.get(m[1]), ...b }; db.recipients.set(r.id, r); return recipient(r); }],
  ['DELETE', /^\/api\/v1\/recipients\/([^/]+)$/, m => { db.recipients.delete(m[1]); return null; }],
  ['POST', /^\/api\/v1\/recipients\/([^/]+)\/occasions$/, (m, b) => { const r = db.recipients.get(m[1]); r.occasions.push({ id: randomUUID(), label: b.typeKey, ...b }); return recipient(r); }],
  ['DELETE', /^\/api\/v1\/recipients\/([^/]+)\/occasions\/([^/]+)$/, m => { const r = db.recipients.get(m[1]); r.occasions = r.occasions.filter(o => o.id !== m[2]); return recipient(r); }],
  ['POST', /^\/api\/v1\/recipients\/([^/]+)\/gift-history$/, (m, b) => { const r = db.recipients.get(m[1]); r.history.push({ id: randomUUID(), ...b }); return recipient(r); }],
  ['GET', /^\/api\/v1\/saved$/, () => [...db.saved].map(([id, s]) => ({ idea: clean(db.ideas.get(id)), recipientId: s.recipientId, recipientNickname: s.recipientId ? db.recipients.get(s.recipientId)?.nickname ?? null : null, savedAt: s.at }))],
  ['GET', /^\/api\/v1\/plans$/, () => [...db.plans.values()]],
  ['GET', /^\/api\/v1\/plans\/([^/]+)$/, m => db.plans.get(m[1]) ?? 404],
  ['PATCH', /^\/api\/v1\/plans\/([^/]+)\/tasks\/([^/]+)$/, (m, b) => { const p = db.plans.get(m[1]); p.tasks = p.tasks.map(t => (t.id === m[2] ? { ...t, done: b.done } : t)); return p; }],
  ['GET', /^\/api\/v1\/me\/export$/, () => ({ recipients: [...db.recipients.values()] })],
  ['DELETE', /^\/api\/v1\/me$/, () => null]
];

createServer(async (req, res) => {
  const url = new URL(req.url, 'http://localhost');
  let body = '';
  for await (const chunk of req) body += chunk;
  const parsed = body ? JSON.parse(body) : {};
  for (const [method, re, fn] of routes) {
    const m = url.pathname.match(re);
    if (m && method === req.method) {
      await new Promise(r => setTimeout(r, 250)); // feel real latency
      const out = fn(m, parsed, url.searchParams);
      if (out === 404) { res.writeHead(404, { 'content-type': 'application/json' }); return res.end('{"title":"not found"}'); }
      if (out === null) { res.writeHead(204); return res.end(); }
      res.writeHead(200, { 'content-type': 'application/json; charset=utf-8' });
      return res.end(JSON.stringify(out));
    }
  }
  res.writeHead(404); res.end();
}).listen(5080, () => console.log('Mock API on http://localhost:5080'));
