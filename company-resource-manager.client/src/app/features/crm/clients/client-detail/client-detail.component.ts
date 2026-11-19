import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClientService } from '../../../../core/services/client.service';
import { Client, ClientContact } from '../../../../core/models/crm.models';

@Component({
  selector: 'app-client-detail',
  standalone: false,
  templateUrl: './client-detail.component.html',
})
export class ClientDetailComponent implements OnInit {
  clientId!: string;
  client: Client | null = null;
  contactColumns = ['fullName', 'email', 'phone', 'jobTitle', 'isPrimary', 'actions'];
  showContactForm = false;
  contactForm!: FormGroup;

  constructor(
    private route: ActivatedRoute,
    private clientService: ClientService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.clientId = this.route.snapshot.paramMap.get('id')!;
    this.loadClient();
    this.contactForm = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', Validators.email],
      phone: [''],
      jobTitle: [''],
      isPrimary: [false],
    });
  }

  loadClient(): void {
    this.clientService.getById(this.clientId).subscribe((c) => {
      this.client = c;
      this.cdr.detectChanges();
    });
  }

  addContact(): void {
    if (this.contactForm.invalid) return;

    const formValue = this.contactForm.value;

    const payload = {
      ...formValue,
      fullName: `${formValue.firstName} ${formValue.lastName}`.trim()
    };

    this.clientService.addContact(this.clientId, payload).subscribe({
      next: () => {
        this.snackBar.open('Contact added', 'Close', { duration: 3000 });
        this.showContactForm = false;
        this.contactForm.reset({ isPrimary: false });
        this.loadClient();
      },
      error: () => this.snackBar.open('Failed to add contact', 'Close', { duration: 3000 }),
    });
  }

  removeContact(contact: ClientContact): void {
    this.clientService.removeContact(this.clientId, contact.id).subscribe({
      next: () => {
        this.snackBar.open('Contact removed', 'Close', { duration: 3000 });
        this.loadClient();
      },
    });
  }
}
