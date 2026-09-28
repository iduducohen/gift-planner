import { Injectable, inject } from '@angular/core';
import { ModalController, ToastController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { SaveSheet } from '../ui/save-sheet';
import { ApiService } from './api.service';
import { AuthService } from './auth';
import { BriefStore } from './brief.store';
import { IdeaDto } from './models';

/** Save / unsave shared by the carousel, grid, detail and saved list (single place for the rules). */
@Injectable({ providedIn: 'root' })
export class IdeaActions {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly brief = inject(BriefStore);
  private readonly toasts = inject(ToastController);
  private readonly modals = inject(ModalController);
  private askedToSignIn = false;

  /** Returns the new saved state. Optimistic callers can revert on failure. */
  async toggleSave(idea: IdeaDto): Promise<boolean> {
    try {
      if (idea.saved) {
        await firstValueFrom(this.api.unsave(idea.id));
        await this.toast('הוסר מהשמורים');
        return false;
      }
      await firstValueFrom(this.api.save(idea.id, this.brief.draft().recipientId));
      if (!this.auth.isSignedIn() && !this.askedToSignIn) {
        // First save as a guest: offer to keep it across devices (never a wall — "not now" is fine).
        this.askedToSignIn = true;
        const m = await this.modals.create({ component: SaveSheet, cssClass: 'sheet' });
        await m.present();
      } else {
        await this.toast('נשמר ❤');
      }
      return true;
    } catch {
      await this.toast('לא הצלחנו לשמור. נסו שוב.');
      return idea.saved;
    }
  }

  async toast(message: string) {
    const t = await this.toasts.create({ message, duration: 1800, position: 'bottom', cssClass: 'gp-toast' });
    await t.present();
  }
}
