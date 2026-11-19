import { NgModule } from '@angular/core';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { SharedModule } from '../../shared/shared.module';
import { ProjectsRoutingModule } from './projects-routing.module';
import { ProjectListComponent } from './project-list/project-list.component';
import { ProjectFormComponent } from './project-form/project-form.component';
import { ProjectDetailComponent } from './project-detail/project-detail.component';
import { KanbanBoardComponent } from './kanban-board/kanban-board.component';
import { TimeEntryListComponent } from './time-entry-list/time-entry-list.component';
import { EstimationWidgetComponent } from './estimation-widget/estimation-widget.component';

@NgModule({
  declarations: [
    ProjectListComponent,
    ProjectFormComponent,
    ProjectDetailComponent,
    KanbanBoardComponent,
    TimeEntryListComponent,
    EstimationWidgetComponent,
  ],
  imports: [SharedModule, ProjectsRoutingModule, DragDropModule],
})
export class ProjectsModule {}
