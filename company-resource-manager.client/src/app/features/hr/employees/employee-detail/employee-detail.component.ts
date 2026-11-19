import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Employee, EmploymentContract, ContractType } from '../../../../core/models/hr.models';
import { EmployeeService } from '../../../../core/services/employee.service';
import { EmploymentContractService } from '../../../../core/services/employment-contract.service';

@Component({
  selector: 'app-employee-detail',
  templateUrl: './employee-detail.component.html',
  standalone: false,
})
export class EmployeeDetailComponent implements OnInit {
  employeeId!: string;
  employee: Employee | null = null;
  contracts: EmploymentContract[] = [];
  contractColumns = ['contractType', 'startDate', 'endDate', 'grossSalary', 'currency', 'isActive', 'actions'];

  showContractForm = false;
  editingContract: EmploymentContract | null = null;
  contractForm!: FormGroup;

  contractTypes: ContractType[] = ['FullTime', 'PartTime', 'Contractor', 'Internship'];

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef,
    private employeeService: EmployeeService,
    private contractService: EmploymentContractService
  ) {}

  ngOnInit(): void {
    this.employeeId = this.route.snapshot.paramMap.get('id')!;
    this.contractForm = this.fb.group({
      contractType: ['FullTime', Validators.required],
      startDate: ['', Validators.required],
      endDate: [null],
      grossSalary: [0, [Validators.required, Validators.min(0)]],
      currency: ['RON', Validators.required],
      isActive: [true],
    });
    this.loadEmployee();
    this.loadContracts();
  }

  loadEmployee(): void {
    this.employeeService.getById(this.employeeId).subscribe((e) => {
      this.employee = e;
      this.cdr.detectChanges();
    });
  }

  loadContracts(): void {
    this.contractService.getByEmployee(this.employeeId).subscribe((list) => {
      this.contracts = list;
      this.cdr.detectChanges();
    });
  }

  openAddForm(): void {
    this.editingContract = null;
    this.contractForm.reset({ contractType: 'FullTime', currency: 'RON', isActive: true, grossSalary: 0 });
    this.showContractForm = true;
  }

  editContract(c: EmploymentContract): void {
    this.editingContract = c;
    this.contractForm.setValue({
      contractType: c.contractType,
      startDate: c.startDate,
      endDate: c.endDate ?? null,
      grossSalary: c.grossSalary,
      currency: c.currency,
      isActive: c.isActive,
    });
    this.showContractForm = true;
  }

  saveContract(): void {
    if (this.contractForm.invalid) return;
    const v = this.contractForm.value;

    if (this.editingContract) {
      this.contractService.update(this.editingContract.id, v).subscribe({
        next: () => {
          this.snackBar.open('Contract updated', 'OK', { duration: 3000 });
          this.showContractForm = false;
          this.cdr.detectChanges();
          this.loadContracts();
        },
        error: () => this.snackBar.open('Failed to update contract', 'OK', { duration: 3000 }),
      });
    } else {
      this.contractService.create({ ...v, employeeId: this.employeeId }).subscribe({
        next: () => {
          this.snackBar.open('Contract created', 'OK', { duration: 3000 });
          this.showContractForm = false;
          this.cdr.detectChanges();
          this.loadContracts();
        },
        error: () => this.snackBar.open('Failed to create contract', 'OK', { duration: 3000 }),
      });
    }
  }

  deleteContract(c: EmploymentContract): void {
    this.contractService.delete(c.id).subscribe({
      next: () => {
        this.snackBar.open('Contract deleted', 'OK', { duration: 3000 });
        this.cdr.detectChanges();
        this.loadContracts();
      },
      error: () => this.snackBar.open('Failed to delete contract', 'OK', { duration: 3000 }),
    });
  }

  cancel(): void {
    this.showContractForm = false;
    this.editingContract = null;
  }
}
