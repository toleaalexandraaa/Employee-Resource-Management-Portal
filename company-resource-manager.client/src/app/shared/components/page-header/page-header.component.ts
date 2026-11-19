import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: false,
  template: `
    <div class="page-header">
      <div class="page-header-content">
        <h1>{{ title }}</h1>
        <p *ngIf="subtitle" class="subtitle">{{ subtitle }}</p>
      </div>
      <div class="page-header-actions">
        <ng-content></ng-content>
      </div>
    </div>
  `,
  styles: [
    `
      .page-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 24px;
      }
      h1 {
        margin: 0;
        font-size: 24px;
        font-weight: 500;
      }
      .subtitle {
        margin: 4px 0 0;
        color: #666;
        font-size: 14px;
      }
      .page-header-actions {
        display: flex;
        gap: 8px;
        align-items: center;
      }
    `,
  ],
})
export class PageHeaderComponent {
  @Input() title = '';
  @Input() subtitle = '';
}
