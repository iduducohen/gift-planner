import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/start/start.page').then(m => m.StartPage), title: 'למי נכין הפתעה?' },
  { path: 'new/about', loadComponent: () => import('./pages/about/about.page').then(m => m.AboutPage), title: 'ספר לי עליה' },
  { path: 'new/budget', loadComponent: () => import('./pages/budget/budget.page').then(m => m.BudgetPage), title: 'תקציב ורגש' },
  { path: 'session/:sessionId/clarify', loadComponent: () => import('./pages/clarify/clarify.page').then(m => m.ClarifyPage), title: 'עוד שאלה אחת' },
  { path: 'session/:sessionId/ideas/:setId', loadComponent: () => import('./pages/ideas/ideas.page').then(m => m.IdeasPage), title: 'רעיונות' },
  { path: 'idea/:id', loadComponent: () => import('./pages/idea/idea.page').then(m => m.IdeaPage), title: 'רעיון' },
  { path: 'plans/:id', loadComponent: () => import('./pages/plan/plan.page').then(m => m.PlanPage), title: 'התוכנית' },
  { path: 'people', loadComponent: () => import('./pages/people/people.page').then(m => m.PeoplePage), title: 'האנשים שלי' },
  { path: 'people/:id', loadComponent: () => import('./pages/person/person.page').then(m => m.PersonPage), title: 'פרופיל' },
  { path: 'saved', loadComponent: () => import('./pages/saved/saved.page').then(m => m.SavedPage), title: 'שמורים' },
  { path: 'sign-in', loadComponent: () => import('./pages/sign-in/sign-in.page').then(m => m.SignInPage), title: 'כניסה' },
  { path: '**', redirectTo: '' }
];
