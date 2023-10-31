import { Component, OnInit, Inject } from '@angular/core';
import {  } from '../models/misc-models';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';

@Component({
  selector: 'app-viewdataextractrequest',
  templateUrl: './viewdataextractrequest.component.html',
  styleUrls: ['./viewdataextractrequest.component.scss']
})

export class ViewDataExtractRequestComponent implements OnInit {

  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  
  constructor(private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
    private router: Router) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;
  }

  ngOnInit() {
    AppInsights.trackPageView("DataExtractRequest component:ngOnInit");

    if (!this.authService.isClaimValid("idp", 'GptwEmployeeLogin')) {
      this._router.navigate(["/error/noaccess"]);
    }

    if (!this.authService.isClaimValid("GptwAd_GptwAppRole_PortalGeneral", "GptwAppRole_PortalGeneral")) {
      this._router.navigate(["/error/noaccess"]);
    }

    //this.getDataExtractRequestById();
  }

  public getDataExtractRequestById(id) {
    //var url = this._baseUrl + 'api/Portal/GetDataExtractRequestById/' + id;
    //this._httpClient.get<GetDataExtractRequestsResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(function (result) {
    //  if (result) {
    //    _this.dataExtractRequests = result.dataExtractRequests;
    //    _this.dataIsLoading = false;
    //  }
    //  else {
    //    _this.dataIsLoading = false;
    //    _this.router.navigate(["/error/general"]);
    //  }
    //}, function (error) {
    //  if (error.status == 401) {
    //    _this.dataIsLoading = false;
    //    _this.router.navigate(["/error/noaccess"]);
    //  }
    //  else {
    //    _this.dataIsLoading = false;
    //    _this.router.navigate(["/error/general"]);
    //  }
    //});
  }
}


