import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProjectListComponent } from './project-list/project-list.component';
import { ProjectFormComponent } from './project-form/project-form.component';
import { ProjectDetailComponent } from './project-detail/project-detail.component';
import { AuthGuard } from '../../core/guards/auth.guard';

const routes: Routes = [
  { path: '', component: ProjectListComponent },
  {
    path: 'new',
    component: ProjectFormComponent,
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'ProjectManager'] },
  },
  {
    path: ':id/edit',
    component: ProjectFormComponent,
    canActivate: [AuthGuard],
    data: { roles: ['Admin', 'ProjectManager'] },
  },
  { path: ':id', component: ProjectDetailComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class ProjectsRoutingModule {}
