import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';

@Component({
  selector: 'app-temp-password-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>Employee Account Created</h2>
    <mat-dialog-content>
      <p>A login account has been created using the work email as username. Share this temporary password with the employee:</p>
      <mat-form-field appearance="outline" style="width:100%;margin-top:8px">
        <mat-label>Temporary Password</mat-label>
        <input matInput [value]="data.password" readonly #pwInput />
        <button matSuffix mat-icon-button (click)="copy(pwInput.value)" matTooltip="Copy to clipboard">
          <mat-icon>content_copy</mat-icon>
        </button>
      </mat-form-field>
      <p><small>The employee should change this password after first login.</small></p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-flat-button color="primary" [mat-dialog-close]="true">Done</button>
    </mat-dialog-actions>
  `,
})
export class TempPasswordDialogComponent {
  constructor(@Inject(MAT_DIALOG_DATA) public data: { password: string }) {}

  copy(text: string): void {
    navigator.clipboard.writeText(text);
  }
}
