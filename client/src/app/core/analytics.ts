import { isDevMode, provideAppInitializer, inject } from '@angular/core';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { computeRoute, inject as injectAnalytics, pageview } from '@vercel/analytics';
import { filter } from 'rxjs';

/**
 * Vercel Web Analytics for this Angular app. The package has no Angular component, so this
 * follows the generic `inject()` setup and reports client-side navigations with the route
 * pattern (`/people/[id]`) the same way the Vue integration does.
 */
export function provideAnalytics() {
  return provideAppInitializer(() => {
    const router = inject(Router);

    injectAnalytics({
      framework: 'angular',
      disableAutoTrack: true,
      mode: isDevMode() ? 'development' : 'production'
    });

    const send = (): void => {
      const path = router.url.split(/[?#]/)[0] || '/';
      pageview({ route: computeRoute(path, routeParams(router)), path });
    };

    let sawNavigation = false;
    router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe(() => {
      sawNavigation = true;
      send();
    });
    if (!sawNavigation && router.navigated) send();
  });
}

function routeParams(router: Router): Record<string, string> {
  const params: Record<string, string> = {};
  let route: ActivatedRouteSnapshot | null = router.routerState.snapshot.root;
  while (route) {
    Object.assign(params, route.params);
    route = route.firstChild;
  }
  return params;
}
