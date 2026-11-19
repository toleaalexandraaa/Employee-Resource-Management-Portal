import { Component } from '@angular/core';

@Component({
  selector: 'app-crm-analytics',
  standalone: false,
  template: `
    <app-page-header
      title="CRM Analytics"
      subtitle="Lead funnel, revenue & client insights"
    ></app-page-header>
    <mat-tab-group animationDuration="150ms" class="crm-tabs">
      <mat-tab label="Leads">
        <ng-template matTabContent>
          <app-leads-tab></app-leads-tab>
        </ng-template>
      </mat-tab>
      <mat-tab label="Revenue">
        <ng-template matTabContent>
          <app-revenue-tab></app-revenue-tab>
        </ng-template>
      </mat-tab>
      <mat-tab label="Clients">
        <ng-template matTabContent>
          <app-clients-tab></app-clients-tab>
        </ng-template>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [
    `
      .crm-tabs {
        margin-top: 16px;
      }
    `,
  ],
})
export class CrmAnalyticsComponent {}
