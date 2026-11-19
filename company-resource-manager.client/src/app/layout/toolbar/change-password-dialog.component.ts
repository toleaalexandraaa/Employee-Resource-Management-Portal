import { ChangeDetectorRef, Component } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ValidationErrors, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { AuthService } from '../../core/services/auth.service';

/** Matches ASP.NET Identity config: min 8, digit, uppercase, lowercase, no special char required */
const PASSWORD_PATTERN = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

@Component({
  selector: 'app-change-password-dialog',
  standalone: false,
  template: `
    <h2 mat-dialog-title>Change Password</h2>
    <mat-dialog-content>
      <form [formGroup]="form" style="display:flex;flex-direction:column;gap:12px;padding-top:8px">
        <mat-form-field appearance="outline">
          <mat-label>Current Password</mat-label>
          <input matInput type="password" formControlName="currentPassword" />
          <mat-error>Required</mat-error>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>New Password</mat-label>
          <input matInput type="password" formControlName="newPassword" />
          <mat-hint>Min 8 chars · uppercase · lowercase · digit</mat-hint>
          @if (form.get('newPassword')?.hasError('required')) {
            <mat-error>Required</mat-error>
          } @else if (form.get('newPassword')?.hasError('minlength')) {
            <mat-error>Minimum 8 characters</mat-error>
          } @else if (form.get('newPassword')?.hasError('pattern')) {
            <mat-error>Must include uppercase, lowercase and a digit</mat-error>
          }
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Confirm New Password</mat-label>
          <input matInput type="password" formControlName="confirmPassword" />
          @if (form.get('confirmPassword')?.hasError('required')) {
            <mat-error>Required</mat-error>
          } @else if (form.hasError('mismatch')) {
            <mat-error>Passwords do not match</mat-error>
          }
        </mat-form-field>
      </form>
      @if (errorMessage) {
        <p style="color:#f44336;margin-top:8px">{{ errorMessage }}</p>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button [mat-dialog-close]="false">Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="loading">
        @if (loading) {
          <mat-spinner diameter="20" style="display:inline-block"></mat-spinner>
        }
        Change Password
      </button>
    </mat-dialog-actions>
  `,
})
export class ChangePasswordDialogComponent {
  form: FormGroup;
  loading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private snackBar: MatSnackBar,
    private dialogRef: MatDialogRef<ChangePasswordDialogComponent>,
    private cdr: ChangeDetectorRef
  ) {
    this.form = this.fb.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(8), Validators.pattern(PASSWORD_PATTERN)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordsMatch }
    );
  }

  private passwordsMatch(group: AbstractControl): ValidationErrors | null {
    const np = group.get('newPassword')?.value;
    const cp = group.get('confirmPassword')?.value;
    return np && cp && np !== cp ? { mismatch: true } : null;
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading = true;
    this.errorMessage = '';
    const { currentPassword, newPassword } = this.form.value;
    this.authService.changePassword(currentPassword, newPassword).subscribe({
      next: () => {
        this.snackBar.open('Password changed successfully', 'Close', { duration: 3000 });
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.errorMessage = err.error?.errors?.[0] ?? 'Failed to change password';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
