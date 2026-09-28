import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { NavController } from '@ionic/angular';
import { IonContent } from '@ionic/angular';
import { IdeaDetail } from '../../ui/idea-detail';

@Component({
  selector: 'app-idea-page',
  imports: [IonContent, IdeaDetail],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<ion-content><main class="detail-wrap"><app-idea-detail [ideaId]="id()" (closed)="back()" /></main></ion-content>`,
  styles: `.detail-wrap { max-width: 720px; margin: 0 auto; }`
})
export class IdeaPage {
  private readonly nav = inject(NavController);
  readonly id = input.required<string>();
  back() { this.nav.back(); }
}
