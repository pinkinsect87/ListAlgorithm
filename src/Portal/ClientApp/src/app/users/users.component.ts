import { Component, OnInit, Inject, ViewEncapsulation, ViewChild } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { DataStateChangeEvent } from '@progress/kendo-angular-grid';
import { State } from '@progress/kendo-data-query';
import { Subject } from 'rxjs';
import { HttpHeaders, HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { PortalContact, GenericResult, AddUpdateContactResult, GetCompanyUsersResult } from '../models/misc-models';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { environment } from '../../environments/environment';
import { TooltipDirective } from '@progress/kendo-angular-tooltip';

const createFormGroup = dataItem => new FormGroup({
  'firstName': new FormControl(dataItem.firstName),
  'lastName': new FormControl(dataItem.lastName),
  'email': new FormControl(dataItem.email),
  'achievementNotification': new FormControl(dataItem.achievementNotification)
});

const ErrorMessage = "An unexpected error has occurred. Please try again. If the problem persists, please contact Customer Support.";

@Component({
  selector: 'app-users',
  encapsulation: ViewEncapsulation.None,
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss']
})

export class UsersComponent implements OnInit {
  @ViewChild(TooltipDirective, { static: true }) public tooltipDir: TooltipDirective;

  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  public gridData: any[];
  public formGroup: FormGroup;
  public editedRowIndex: number;
  private clientId: number;
  public currentYear: number = 2019;
  public removeConfirmationSubject: Subject<boolean> = new Subject<boolean>();
  public itemToRemove: any;
  public dialogOpened = false;
  public windowOpened = false;
  public confirmOpened: boolean = false;
  public actionsLayout: string = 'normal';
  public savedDataItem: any = null;
  public savedAchievementNotificationState: boolean;
  public actionTaken: string = "";
  public emailToDelete: string = "";
  public errorMessage: string = "";
  public dataIsLoading: boolean = false;
  public noRecordstoDisplayText: string = "";
  public hasTenantIdClaim: boolean = false;

  public myActions = [
    { text: 'No' },
    { text: 'Yes', primary: true }
  ]

  constructor(private route: ActivatedRoute, private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
    private router: Router, public bs: BackendService) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;
    route.params.subscribe(params => {
      this.clientId = params['clientId'];
    })
  }
  public ngOnInit(): void {

    this.bs.setHeaderDependencies("clientId", this.clientId);

    this.dataIsLoading = true;
    let url: string = this._baseUrl + 'api/Users/GetCompanyUsers?clientId=' + this.clientId;
    //url = encodeURI(url)
    this._httpClient.get<GetCompanyUsersResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      this.dataIsLoading = false;
      if (result) {
        if (result.isError)
          this.open('dialog', result.errorStr);
        else {
          this.gridData = result.portalContacts;
          var ukgContacts = result.portalContacts.filter(p => p.hasTenantIdClaim);
          if (ukgContacts && ukgContacts != null && ukgContacts.length > 0) {
            this.hasTenantIdClaim = true;
          }
        }
      }
      else {
        //this.open('dialog', "GetCompanyUsers failed with error: result of controller call is undefined");
        this.router.navigate(["/error/general"]);
      }
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });
  }

  public onConfirmAction(action: DialogAction): void {
    console.log(action);
    if (action.text == "Yes") {
      this.removeHandler2();
    }
    else {
      this.confirmOpened = false;
      this.dataIsLoading = false;
    }
  }

  public onConfirmClose(status) {
    this.confirmOpened = false;
  }

  public addHandler({ sender }) {
    this.actionTaken = "add";
    this.closeEditor(sender);

    this.formGroup = createFormGroup({
      'firstName': '',
      'lastName': '',
      'email': '',
      'achievementNotification': true
    });

    sender.addRow(this.formGroup);
  }

  public editHandler({ sender, rowIndex, dataItem }) {
    this.actionTaken = "edit";
    this.savedAchievementNotificationState = dataItem.achievementNotification;

    this.closeEditor(sender);

    this.formGroup = createFormGroup(dataItem);

    this.editedRowIndex = rowIndex;

    sender.editRow(rowIndex, this.formGroup);
  }

  public cancelHandler({ sender, rowIndex }) {
    if (this.actionTaken == "edit") {
      this.gridData[rowIndex].achievementNotification = this.savedAchievementNotificationState;
      this.actionTaken = "";
    }
    this.closeEditor(sender, rowIndex);
  }

  public saveHandler({ sender, rowIndex, formGroup, isNew }): void {
    this.actionTaken = "save";
    const user = formGroup.value;

    if (user.firstName == undefined)
      user.firstName = "";

    if (user.lastName == undefined)
      user.lastName = "";

    this.dataIsLoading = true;

    var url: string = "";

    if (isNew == false && rowIndex >= 0)
      user.achievementNotification = this.gridData[rowIndex].achievementNotification;

    if (isNew) {
      for (let i = 0; i <= this.gridData.length - 1; i++) {
        if (this.gridData[i].email.toLowerCase() == user.email.trim().toLowerCase()) {
          this.open('dialog', "<DISPLAYTOUSER>This email address is currently in use.");
          sender.closeRow(rowIndex);
          return;
        }
      }
    }

    if (isNew)
      url = this._baseUrl + 'api/Users/AddCompanyUser';
    else
      url = this._baseUrl + 'api/Users/UpdateCompanyUser';

    url += '?clientId=' + this.clientId + '&firstName=' + user.firstName.trim() + '&lastName=' + user.lastName.trim() + '&AchievementNotification=' + user.achievementNotification;
    url += '&email=' + encodeURIComponent(user.email.trim());

    this._httpClient.get<AddUpdateContactResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {

      this.dataIsLoading = false;

      if (result) {
        if (result.isError) {
          if (result.errorId == 1)
            this.open('dialog', "<DISPLAYTOUSER>This email address is already in use and cannot be added again.  If you required further assistance please contact us.");
          else if (result.errorId == 2)
            this.open('dialog', "<DISPLAYTOUSER>This email address is not allowed as it contains either 'greatplacetowork' or 'gptw'. If you require further assistance please contact Customer Support.");
          else
            this.open('dialog', result.errorStr);
        }
        else {
          //this.gridData = result.portalContacts;

          if (isNew) {
            this.gridData.splice(0, 0, user);
          } else {

            Object.assign(
              this.gridData.find(({ email }) => email === user.email),
              user
            );
          }
        }
      }
      else {
        this.open('dialog', "GetCompanyUsers failed with error: result of controller call is undefined");
      }
    }, error => {
      this.open('dialog', "GetCompanyUsers failed with an unhandled error.");
    });

    //this.bs.save(user, isNew);

    sender.closeRow(rowIndex);
  }

  public removeHandler({ dataItem }): void {
    let numUsers: number = this.gridData.length;

    if (numUsers <= 1) {
      this.open('dialog', "<DISPLAYTOUSER>At least one user needs to exist at all times. In order to delete this specific user, first create another user and you will then be able to delete this user.");
    }
    else {
      if (!this.confirmOpened) {
        this.emailToDelete = dataItem.email;
        this.confirmOpened = true;
        this.savedDataItem = dataItem;
        this.dataIsLoading = true;
      }
    }
  }

  public removeHandler2(): void {

    this.confirmOpened = false;
    this.dataIsLoading = true;
    let url: string = this._baseUrl + 'api/Users/DeleteCompanyUser?clientId=' + this.clientId
    url += '&email=' + encodeURIComponent(this.savedDataItem.email);
    this._httpClient.get<GenericResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {

      this.dataIsLoading = false;

      if (result) {
        if (result.isError) {
          this.open('dialog', result.errorStr);
        }
        else {
          const index = this.gridData.findIndex(({ email }) => email === this.savedDataItem.email);
          this.gridData.splice(index, 1);
          //this.open('dialog')
          //this.gridData = result.portalContacts;
        }
      }
      else {
        this.open('dialog', "GetCompanyUsers failed with error: result of controller call is undefined");
      }
    }, error => {
      this.open('dialog', "GetCompanyUsers failed with an unhandled error.");
    });
  }

  private closeEditor(grid, rowIndex = this.editedRowIndex) {
    grid.closeRow(rowIndex);
    this.editedRowIndex = undefined;
    this.formGroup = undefined;
  }

  public close(component) {
    this[component + 'Opened'] = false;
  }

  public getToolTipText(title) {
    if (title == "First Name")
      return "Users First Name. (required)";
    if (title == "Last Name")
      return "Users Last Name. (required)";
    if (title == "Email")
      return "Users Email Address. (required)";
    if (title == "Achievement Notification")
      return "Get notified when you make a Best Workplace list";
  }

  public open(component, errorMessage) {
    this.dataIsLoading = false;
    console.error(errorMessage);
    this.errorMessage = this.normalizeError(errorMessage)
    this[component + 'Opened'] = true;
  }

  public action(status) {
    console.log(`Dialog result: ${status}`);
    this.dialogOpened = false;
  }

  private normalizeError(errorMessage) {
    if (errorMessage.indexOf("<DISPLAYTOUSER>") == 0) {
      return errorMessage.substring(15);
    }

    //if (!environment.production)
    //    return errorMessage;

    return ErrorMessage;
  }

  public showTooltip(e: MouseEvent): void {
    const element = e.target as HTMLElement;
    if ((element.nodeName === "TH" || element.nodeName === "TD") && element.offsetWidth < element.scrollWidth) {
      this.tooltipDir.toggle(element);
    } else {
      this.tooltipDir.hide();
    }
  }
}

