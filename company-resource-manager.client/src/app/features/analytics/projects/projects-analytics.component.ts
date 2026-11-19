import { Component } from '@angular/core';

@Component({
  selector: 'app-projects-analytics',
  standalone: false,
  template: `
    <app-page-header
      title="Project Analytics"
      subtitle="Health, time tracking & task metrics"
    ></app-page-header>
    <mat-tab-group animationDuration="150ms" class="proj-tabs">
      <mat-tab label="Health">
        <ng-template matTabContent>
          <app-health-tab></app-health-tab>
        </ng-template>
      </mat-tab>
      <mat-tab label="Time Tracking">
        <ng-template matTabContent>
          <app-time-tab></app-time-tab>
        </ng-template>
      </mat-tab>
      <mat-tab label="Tasks">
        <ng-template matTabContent>
          <app-tasks-tab></app-tasks-tab>
        </ng-template>
      </mat-tab>
    </mat-tab-group>
  `,
  styles: [
    `
      .proj-tabs {
        margin-top: 16px;
      }
    `,
  ],
})
export class ProjectsAnalyticsComponent {}
