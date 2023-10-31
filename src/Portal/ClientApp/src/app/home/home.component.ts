import { Component, Inject } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Claim, FindCurrentEngagementInfoResult } from '../models/misc-models';
import { AppInsights } from 'applicationinsights-js';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})

export class HomeComponent {
  public result: string;
  //public claims: Claim[];
  public engagementResult: FindCurrentEngagementInfoResult = new FindCurrentEngagementInfoResult;
  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;

  constructor(private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private router: Router) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;
  }

  ngOnInit() {

    AppInsights.trackPageView("Home component:ngOnInit");

    if (this.authService.user) {
      let idpClaim = this.authService.getNonRoleClaim("idp");
      let clientIdClaim = this.authService.getNonRoleClaim("gptw_client_id");

      if (idpClaim == 'GptwEmployeeLogin') {

        const affiliateCode: string = this.authService.getUS1OrTopAlphaAffiliateCode();

        if (affiliateCode.length == 0) {
          this._router.navigate(["/error/noaccess"]);
        }

        this._router.navigate(["/employee/dashboard/"]);
        //this._router.navigate(["/gptwemployee"]);
        //this._router.navigate(["/admin/newecr/done"]);

        return;
      }

      if ((idpClaim == "local" || idpClaim == "UkgSamlLogin" ) && clientIdClaim.length) {
        this._httpClient.get<FindCurrentEngagementInfoResult>(this._baseUrl + 'api/Portal/FindCurrentEngagementInfo?propertyName=clientId&property=' + clientIdClaim, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
          this.engagementResult = result;
          if (result) {
            if (this.engagementResult.errorOccurred) {
              if (!this.engagementResult.foundActiveEngagement)
                this._router.navigate(["/error/noengagement"]);
              else
                this._router.navigate(["/error/general"]);
            }
            else {
              this.result = "/certify/client/" + this.engagementResult.clientId;
              this._router.navigate([this.result]);
            }
          }
          else {
            this._router.navigate(["/error/general"]);
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
      else {
        this._router.navigate(["/error/noportalclaim"]);
      }

    }
  }

}
