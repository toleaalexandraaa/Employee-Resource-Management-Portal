import { Component } from '@angular/core';

@Component({
  selector: 'app-hr-analytics',
  standalone: false,
  template: `
    <app-page-header title="HR Analytics" subtitle="Workforce, leave & payroll insights"></app-page-header>
    <mat-tab-group animationDuration="150ms" class="hr-tabs">
      <mat-tab label="Workforce">
        <ng-template matTabContent>
          <app-workforce-tab></app-workforce-tab>
        </ng-template>
      </mat-tab>
      <mat-tab label="Leave">
        <ng-template matTabContent>
          <app-leave-tab></app-leave-tab>
        </ng-template>
      </mat-tab>
      <mat-tab label="Payroll">
        <ng-template matTabContent>
          <app-payroll-tab></app-payroll-tab>
        </ng-template>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [
    `
      .hr-tabs {
        margin-top: 16px;
      }
    `,
  ],
})
export class HrAnalyticsComponent {}
