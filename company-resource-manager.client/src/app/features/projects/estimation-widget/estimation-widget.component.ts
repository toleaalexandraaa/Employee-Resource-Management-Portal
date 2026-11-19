import { ChangeDetectorRef, Component, Input, OnChanges, OnDestroy, OnInit, SimpleChanges } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Subject, debounceTime, switchMap, catchError, of, takeUntil } from 'rxjs';
import { EstimationService } from '../../../core/services/estimation.service';
import { EstimateProjectResponse } from '../../../core/models/estimation.models';

@Component({
  selector: 'app-estimation-widget',
  standalone: false,
  templateUrl: './estimation-widget.component.html',
})
export class EstimationWidgetComponent implements OnInit, OnChanges, OnDestroy {
  @Input() hasFixedDeadline = false;

  form!: FormGroup;
  result: EstimateProjectResponse | null = null;
  loading = false;

  private readonly trigger$ = new Subject<void>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private estimationService: EstimationService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      teamSize: [3],
      estimatedHoursTotal: [160],
      taskCount: [10],
      averageSeniorityYears: [3],
      isFirstProjectWithClient: [false],
    });

    // Debounce form changes into the trigger stream
    this.form.valueChanges.pipe(debounceTime(500), takeUntil(this.destroy$)).subscribe(() => {
      this.trigger$.next();
    });

    // Single switchMap subscription — cancels in-flight requests on retrigger
    this.trigger$
      .pipe(
        takeUntil(this.destroy$),
        switchMap(() => {
          this.loading = true;
          const val = this.form.value;
          this.cdr.detectChanges();
          return this.estimationService
            .estimate({
              teamSize: Math.max(1, val.teamSize || 1),
              estimatedHoursTotal: Math.max(0, val.estimatedHoursTotal || 0),
              taskCount: Math.max(0, val.taskCount || 0),
              hasFixedDeadline: this.hasFixedDeadline,
              averageSeniorityYears: Math.max(0, val.averageSeniorityYears || 0),
              isFirstProjectWithClient: !!val.isFirstProjectWithClient,
            })
            .pipe(catchError(() => of(null)));
        }),
      )
      .subscribe((res) => {
        this.loading = false;
        this.result = res;
        this.cdr.detectChanges();
      });

    // Run on open
    this.trigger$.next();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['hasFixedDeadline'] && !changes['hasFixedDeadline'].firstChange && this.trigger$) {
      this.trigger$.next();
      this.cdr.detectChanges();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
