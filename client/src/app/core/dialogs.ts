import { Injectable, inject } from '@angular/core';
import { AlertController } from '@ionic/angular';

/** Small native-feeling dialogs (Ionic alerts) instead of window.prompt / confirm. */
@Injectable({ providedIn: 'root' })
export class Dialogs {
  private readonly alerts = inject(AlertController);

  async askText(header: string, message: string, value = '', placeholder = ''): Promise<string | null> {
    const a = await this.alerts.create({
      header, message,
      inputs: [{ name: 'v', type: 'text', value, placeholder, attributes: { maxlength: 40, autocapitalize: 'off' } }],
      buttons: [{ text: 'ביטול', role: 'cancel' }, { text: 'שמירה', role: 'confirm' }]
    });
    await a.present();
    const { data, role } = await a.onDidDismiss<{ values: { v: string } }>();
    const v = data?.values?.v?.trim();
    return role === 'confirm' && v ? v : null;
  }

  async confirm(header: string, message: string, ok = 'אישור', destructive = false): Promise<boolean> {
    const a = await this.alerts.create({
      header, message,
      buttons: [{ text: 'ביטול', role: 'cancel' }, { text: ok, role: destructive ? 'destructive' : 'confirm' }]
    });
    await a.present();
    const { role } = await a.onDidDismiss();
    return role === 'confirm' || role === 'destructive';
  }
}
