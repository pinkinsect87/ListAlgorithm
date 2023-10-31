import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { WindowRef } from './services/auth.service';
import { PortalClaimRequired } from './services/auth.service';
import { GPTWEmployeeOnly } from './services/auth.service';
import { AuthenticationOnly } from './services/auth.service';
import { OpenAccess } from './services/auth.service';
import { AppComponent } from './app.component';
import { NavMenuComponent } from './nav-menu/nav-menu.component';
import { HomeComponent } from './home/home.component';
import { AuthGuardService } from './services/auth-guard.service';
import { AuthService } from './services/auth.service';
import { AuthCallbackComponent } from './auth-callback/auth-callback.component';
import { LocationStrategy, APP_BASE_HREF, PathLocationStrategy } from '@angular/common';
import { EngagementComponent } from './engagement/engagement.component';
import { ErrorComponent } from './error/error.component'
import { NewecrComponent } from './newecr/newecr.component'
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { BackendService } from './services/backend.service';
import { HeaderComponent } from './header/header.component';
import { ReportsComponent } from './reports/reports.component';
import { NewECRDoneComponent } from './newecrdone/newecrdone.component';
import { TreeViewModule } from '@progress/kendo-angular-treeview';
import { UsersComponent } from './users/users.component';
import { GridModule } from '@progress/kendo-angular-grid';
import { RecognitionComponent } from './recognition/recognition.component';
import { CertificationToolkitComponent } from './certificationtoolkit/certificationtoolkit.component';
import { DialogsModule } from '@progress/kendo-angular-dialog';
import { HttpClient, HttpClientModule, HttpClientJsonpModule } from '@angular/common/http';
import { PopupModule } from '@progress/kendo-angular-popup';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { DropDownListModule } from '@progress/kendo-angular-dropdowns';
import { PopupAnchorDirective } from './popup.anchor-target.directive';
import { ReactiveFormsModule } from '@angular/forms';
import { FooterComponent } from './footer/footer.component';
import { ListcalendarComponent } from './listcalendar/listcalendar.component';
import { TooltipModule } from '@progress/kendo-angular-tooltip';
import { DatePipe } from '@angular/common';
import { SortDescriptor, orderBy } from '@progress/kendo-data-query';
import { ImageGalleryComponent } from './image-gallery/image-gallery.component';
import { ScrollViewModule } from '@progress/kendo-angular-scrollview';
import { HelpdeskssoComponent } from './helpdesksso/helpdesksso.component';
import { LogoutComponent } from './logout/logout.component';
import { ImageuploaderComponent } from './imageuploader/imageuploader.component';
import { UploadModule } from '@progress/kendo-angular-upload';
import { SortableModule } from '@progress/kendo-angular-sortable';
import { PhotoComponent } from './photo/photo.component';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { SurveyfeedbackComponent } from './surveyfeedback/surveyfeedback.component';
import { ClientComponent } from './client/client.component';
import { GptwemployeeComponent } from './gptwemployee/gptwemployee.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { MultiCheckFilterComponent } from './multicheck-filter/multicheck-filter.component';
import { CountriesComponent } from './countries/countries.component';
import { InternationalComponent } from './international/international.component';
import { MenusModule } from '@progress/kendo-angular-menu';
import { LabelModule } from '@progress/kendo-angular-label';
import { QuickLinksComponent } from './engagement-components/quick-links/quick-links.component';
import { HeroMessageComponent } from './engagement-components/hero-message/hero-message.component';
import { EmailDashboardComponent } from './emaildashboard/emaildashboard.component';
import { DataRequestDashboardComponent } from './datarequestdashboard/datarequestdashboard.component';
import { NewEcrComponent2 } from './newecr2/newecr2.component';
import { SurveyCountryComponent } from './newecr2-components/survey-country/survey-country.component';
import { IndicatorsModule } from "@progress/kendo-angular-indicators";
import { ListRequestComponent } from './listrequest/listrequest.component';

@NgModule({
  declarations: [
    AppComponent,
    NavMenuComponent,
    HomeComponent,
    AuthCallbackComponent,
    EngagementComponent,
    ErrorComponent,
    NewecrComponent,
    HeaderComponent,
    ReportsComponent,
    NewECRDoneComponent,
    UsersComponent,
    RecognitionComponent,
    CertificationToolkitComponent,
    UsersComponent,
    PopupAnchorDirective,
    FooterComponent,
    ListcalendarComponent,
    ImageGalleryComponent,
    HelpdeskssoComponent,
    LogoutComponent,
    ImageuploaderComponent,
    PhotoComponent,
    SurveyfeedbackComponent,
    ClientComponent,
    GptwemployeeComponent,
    DashboardComponent,
    MultiCheckFilterComponent,
    CountriesComponent,
    InternationalComponent,
    QuickLinksComponent,
    HeroMessageComponent,
    EmailDashboardComponent,
    DataRequestDashboardComponent,
    NewEcrComponent2,
    SurveyCountryComponent,
    ListRequestComponent
  ],
  imports: [
    BrowserModule.withServerTransition({ appId: 'ng-cli-universal' }),
    HttpClientModule,
    FormsModule,
    HttpClientJsonpModule,
    RouterModule.forRoot([
      { path: '', component: HomeComponent, pathMatch: 'full', canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'recognition/view/:clientId', component: RecognitionComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'certtoolkit/view/:clientId', component: CertificationToolkitComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'reports/view/:clientId', component: ReportsComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'users/:clientId', component: UsersComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'admin/newecr', component: NewecrComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'admin/newecr2', component: NewEcrComponent2, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'admin/newecr/done', component: NewECRDoneComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'certify/engagement/:engagementId/:status', component: EngagementComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'certify/engagement/:engagementId', component: EngagementComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'certify/client/:clientId', component: ClientComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'error/:errorType', component: ErrorComponent, canActivate: [AuthGuardService], data: { access: OpenAccess } },
      { path: 'auth-callback', component: AuthCallbackComponent },
      { path: 'listcalendar/:clientId', component: ListcalendarComponent, canActivate: [AuthGuardService], data: { access: PortalClaimRequired } },
      { path: 'helpdesksso', component: HelpdeskssoComponent, canActivate: [AuthGuardService], data: { access: AuthenticationOnly } },
      { path: 'helpdesksso2', component: HelpdeskssoComponent, canActivate: [AuthGuardService], data: { access: AuthenticationOnly } },
      { path: 'logout', component: LogoutComponent, canActivate: [AuthGuardService], data: { access: OpenAccess } },
      { path: 'logout2', component: LogoutComponent, canActivate: [AuthGuardService], data: { access: OpenAccess } },
      { path: 'manageprofile/view/:clientId', component: PhotoComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'gptwemployee', component: GptwemployeeComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'gptwemployee/:pageType', component: GptwemployeeComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'employee/dashboard/:affiliateId', component: DashboardComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'employee/dashboard', component: DashboardComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'admin/emaildashboard/:engagementId', component: EmailDashboardComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'admin/datarequestdashboard', component: DataRequestDashboardComponent, canActivate: [AuthGuardService], data: { access: GPTWEmployeeOnly } },
      { path: 'listrequests', component: ListRequestComponent, canActivate: [AuthGuardService], data: { access: OpenAccess } },
      { path: '**', redirectTo: 'error/general' }
    ], { relativeLinkResolution: 'legacy' }),
    DropDownsModule,
    BrowserAnimationsModule,
    TreeViewModule,
    GridModule,
    DialogsModule,
    DropDownListModule,
    PopupModule,
    InputsModule,
    ReactiveFormsModule,
    TooltipModule,
    ScrollViewModule,
    UploadModule,
    SortableModule,
    ButtonsModule,
    MenusModule,
    LabelModule,
    IndicatorsModule
  ],
  providers: [AuthGuardService, AuthService, WindowRef, DatePipe,
    { provide: LocationStrategy, useClass: PathLocationStrategy },
    { provide: BackendService, useClass: BackendService },
    { provide: APP_BASE_HREF, useValue: '/' }],
  bootstrap: [AppComponent]
})

export class AppModule { }
