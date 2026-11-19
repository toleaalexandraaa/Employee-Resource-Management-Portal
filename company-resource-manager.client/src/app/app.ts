import { Component } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-root',
  standalone: false,
  template: '<router-outlet></router-outlet>',
})
export class App {
  constructor(private translate: TranslateService) {
    translate.setDefaultLang('en');
    const savedLang = localStorage.getItem('app_lang') || 'en';
    translate.use(savedLang);
  }
}
