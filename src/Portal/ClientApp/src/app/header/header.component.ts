import { Component, OnInit, Input, Inject } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';
import { GetDataExtractRequestsResult, DataExtractRequest, HeaderNotifyEvent } from '../models/misc-models';
import { HttpClient } from '@angular/common/http';
import { saveAs } from 'file-saver';

enum DataRequestJobStatus {
  CREATED = "Created",
  IN_PROGRESS = "In Progress",
  COMPLETE = "Complete",
  FAILED = "Failed",
  CANCELLED = "Cancelled",
}

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})

export class HeaderComponent implements OnInit {
  @Input() hideOverviewLink: boolean;
  @Input() hideCompanyUsersLink: boolean;
  public bs: BackendService;
  private as: AuthService;
  private httpClient: HttpClient;
  public baseurl: string;
  public showOverviewLink = true;
  public showCompanyUsersLink = true;
  public isEmployee = false;
  public showViewDataRequests = false;
  public dataIsLoading = false;
  public isOpenDataExtractRequest: boolean = false;
  public dataExtractRequests: any[] = [];

  constructor(bs: BackendService, public authService: AuthService,
    http: HttpClient, @Inject('BASE_URL') baseUrl: string, private router: Router) {
    this.httpClient = http;
    this.baseurl = baseUrl;
    this.bs = bs;
    this.as = authService;
  }

  ngOnInit() {
    if (this.hideOverviewLink)
      this.showOverviewLink = false;
    if (this.hideCompanyUsersLink)
      this.showCompanyUsersLink = false;
    if (this.as.isEmployee()) {
      this.isEmployee = true;
      if (this.authService.doesEmployeeHaveDevEnvClaim())
        this.showViewDataRequests = true;
    }
    else
      this.isEmployee = false;
    console.log("isEmployee = " + this.isEmployee);
  }

  onClickRoute(routeStr: string) {
    switch (routeStr) {
      case "users": {
        this.router.navigate(["/users/" + this.bs.headerInfo.clientId]);
        break;
      }
      case "help": {
        this.bs.navigateExtNewWindow("https://support.greatplacetowork.com/");
        break;
      }
      case "logOut": {
        this.as.logOut();
        break;
      }
      case "overview": {
        this.router.navigate(["/certify/engagement/" + this.bs.headerInfo.engagementId]);
        break;
      }
      case "dashboard": {
        this.router.navigate(["/employee/dashboard/"]);
        break;
      }
      case "dataextractrequest": {
        this.isOpenDataExtractRequest = true;
        this.getDataExtractRequestByEmail();
        break;
      }
    }
  }

  closeDataExtractRequest() {
    this.isOpenDataExtractRequest = false;
  }

  public onLinkClick(dataItem: DataExtractRequest) {
    let email = this.authService.getNonRoleClaim("upn");
    this.dataIsLoading = true;
    let url = 'api/Portal/DownloadDataRequest?id=' + dataItem.id;
    this.httpClient.get<Blob>(url, { headers: this.authService.getRequestHeaders(), responseType: 'blob' as 'json' }).subscribe(result => {
      var test = result;
      var blob = new Blob([result], { type: 'application/octet-stream' });
      saveAs(blob, dataItem.link);
      this.dataIsLoading = false;
    }, error => {
      console.error(error);
      this.router.navigate(["/error/general"]);
    }
    )
}



  public getLinkText(dataItem: DataExtractRequest) {
    if (dataItem.status == DataRequestJobStatus.COMPLETE)
      return "Download"
    else return ""
  }

  public getDataExtractRequestByEmail() {
    this.dataIsLoading = true;
    var _this = this;

    var url = this.baseurl + 'api/Portal/GetDataExtractRequests';
    this.httpClient.get<GetDataExtractRequestsResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(function (result) {
      if (result) {
        _this.dataExtractRequests = result.dataExtractRequests;
        _this.dataIsLoading = false;
      }
      else {
        _this.dataIsLoading = false;
        _this.router.navigate(["/error/general"]);
      }
    }, function (error) {
      if (error.status == 401) {
        _this.dataIsLoading = false;
        _this.router.navigate(["/error/noaccess"]);
      }
      else {
        _this.dataIsLoading = false;
        _this.router.navigate(["/error/general"]);
      }
    });
  }
}
