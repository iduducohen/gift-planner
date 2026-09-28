import { Injectable, inject } from '@angular/core';
import { ModalController } from '@ionic/angular';
import { firstValueFrom } from 'rxjs';
import { InterestsPickerModal } from '../ui/interests-picker.modal';
import { OptionPickerModal } from '../ui/option-picker.modal';
import { ApiService } from './api.service';
import { OptionDto, OptionGroupDto } from './models';

/** Opens the picker modals with data from the taxonomy API. */
@Injectable({ providedIn: 'root' })
export class Pickers {
  private readonly modals = inject(ModalController);
  private readonly api = inject(ApiService);

  async relationship(selectedKey: string | null): Promise<OptionDto | null> {
    const data = await firstValueFrom(this.api.relationships$);
    return this.single('למי המתנה?', data.common, data.groups, selectedKey, 'חיפוש: אחיינית, מנהלת, שכנים…');
  }

  async occasion(selectedKey: string | null): Promise<OptionDto | null> {
    const data = await firstValueFrom(this.api.occasions$);
    return this.single('לאיזה אירוע?', data.common, data.groups, selectedKey, 'חיפוש: חנוכת בית, פרישה…');
  }

  async ageRange(selectedKey: string | null): Promise<OptionDto | null> {
    const groups = await firstValueFrom(this.api.ageRanges$);
    return this.single('בת כמה?', [], groups, selectedKey, '', false);
  }

  async interests(relationship: string | null, ageRange: string | null, initial: string[]): Promise<string[] | null> {
    const data = await firstValueFrom(this.api.interests(relationship, ageRange));
    const m = await this.modals.create({ component: InterestsPickerModal, componentProps: { data, initial }, cssClass: 'picker' });
    await m.present();
    const { data: result, role } = await m.onWillDismiss<string[]>();
    return role === 'select' ? (result ?? []) : null;
  }

  private async single(title: string, common: OptionDto[], groups: OptionGroupDto[], selectedKey: string | null,
                       searchPlaceholder: string, searchable = true): Promise<OptionDto | null> {
    const m = await this.modals.create({
      component: OptionPickerModal,
      componentProps: { title, common, groups, selectedKey, searchPlaceholder, searchable },
      cssClass: 'picker'
    });
    await m.present();
    const { data, role } = await m.onWillDismiss<OptionDto>();
    return role === 'select' ? data ?? null : null;
  }
}
