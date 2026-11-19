import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { ShellComponent } from './shell/shell.component';
import { SidenavComponent } from './sidenav/sidenav.component';
import { ToolbarComponent } from './toolbar/toolbar.component';
import { ChangePasswordDialogComponent } from './toolbar/change-password-dialog.component';

@NgModule({
  declarations: [ShellComponent, SidenavComponent, ToolbarComponent, ChangePasswordDialogComponent],
  imports: [SharedModule, RouterModule],
  exports: [ShellComponent],
})
export class LayoutModule {}
