import { Injectable, Inject } from '@angular/core';
import { UserManager, UserManagerSettings, User } from 'oidc-client';
import { AppInsights } from 'applicationinsights-js';
import { Router } from '@angular/router';
import { ClientConfiguration, Claim, SetPortalContactLoginDateResult, AppConfigDetailsResult } from '../models/misc-models';
import { HttpClient, HttpHeaders, HttpParameterCodec } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})

export class AuthService {
  public manager: UserManager;
  public claims: Claim[];
  public user: User = null;
  private baseUrl: string = "";
  private httpClient: HttpClient;
  public config: ClientConfiguration = new ClientConfiguration();
  public nativeWindow: any;
  private sessionId: string = "";
  public timerId: any;
  public isAppMaintenanceMode: string = "";
  public appVersion: string = null;

  constructor(private winRef: WindowRef, @Inject('BASE_URL') baseUrl: string,
    private router: Router, http: HttpClient) {
    this.nativeWindow = winRef.nativeWindow;
    this.config = winRef.nativeWindow.client_config;
    this.baseUrl = baseUrl;
    this.httpClient = http;
    //this.manager = new UserManager(getClientSettings(baseUrl, this.config.AuthServerAuthority));
    this.manager = new UserManager(getClientSettings(baseUrl, this.config.AuthServerAuthority));
    //this.manager.getUser().then(user => {
    //  this.user = user;
    //});

    let now = parseInt((Date.now() / 1000).toString());
    let expires_at = now + 3600;

    var json:any = {
      "id_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY4MjZCMzIwNDY4MTg5Njc0RUVCRjdFQzI4QjFEQzUxQkY5RUIxN0VSUzI1NiIsIng1dCI6ImFDYXpJRWFCaVdkTzZfZnNLTEhjVWItZXNYNCIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2RldmVsb3BtZW50LWxvZ2luLmdyZWF0cGxhY2V0b3dvcmsuY29tL2lkZW50aXR5IiwibmJmIjoxNjk3NjM2ODY4LCJpYXQiOjE2OTc2MzY4NjgsImV4cCI6MTY5NzY2NTY2OCwiYXVkIjoiR3B0d19Qb3J0YWwiLCJhbXIiOlsicHdkIl0sIm5vbmNlIjoiYmFhOGU0MjViZjZiNGRlMWJkMjczZThhNjMwNGI3OTIiLCJhdF9oYXNoIjoidUtRODEtdE1aLTV5VGIwSlV0WTBWdyIsInNpZCI6IjlCODg2NjdFNEY2QzAwQjhDNDc2MzMwQzVBNUZEMzVCIiwic3ViIjoiMGEzZTNkZjgtZGRkZi00NDAzLWEzM2QtMjBhZjg4ZmEwOTBhIiwiYXV0aF90aW1lIjoxNjk3NjM1MjY3LCJpZHAiOiJsb2NhbCJ9.brM9esJQPZ3X6fxHJukEBt0kfH4MLAERAD5S41GtphnCIBAcRYhfK_Myyn4CSlAyF7t_8mDVi1HOdkEzjUetsv8gGOJcMW40U67L9EuJT1JOOKPgJSrTtwJ5GDrRbUIod0HeT2GXfwwZiREsJD8vwcR52Fb-Qbge1E2gvn9nQDjxr-xSH9FIlqKIYnE0O54JhZ3_HR2F5NKeILN1g-rR83SaaWbEMYrhUh7S_qAPuYsJjqV0zi6HwJ-_P-bRdSPAqqP83YxFDdsqp1oDstlXlFns9ql1ONkm4n916a2_wpK82Ph5NE4jNsyxeSMe-36TUwGoxFApKXggq-P4HOr9mQ",
      "session_state": "O3YXbEiJBg6vAA6zhjWLXyur2SJgbpm__x-IRcTHP40.D9D9201C352C5002FE8AEB5C63253AC0",
      "access_token": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY4MjZCMzIwNDY4MTg5Njc0RUVCRjdFQzI4QjFEQzUxQkY5RUIxN0VSUzI1NiIsIng1dCI6ImFDYXpJRWFCaVdkTzZfZnNLTEhjVWItZXNYNCIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2RldmVsb3BtZW50LWxvZ2luLmdyZWF0cGxhY2V0b3dvcmsuY29tL2lkZW50aXR5IiwibmJmIjoxNjk3NjM2ODY4LCJpYXQiOjE2OTc2MzY4NjgsImV4cCI6MTY5NzY2NTY2OCwiYXVkIjoiaHR0cHM6Ly9kZXZlbG9wbWVudC1sb2dpbi5ncmVhdHBsYWNldG93b3JrLmNvbS9pZGVudGl0eS9yZXNvdXJjZXMiLCJzY29wZSI6WyJvcGVuaWQiLCJwcm9maWxlIiwiYWxsX2NsYWltcyIsIkdwdHdDbGllbnRMb2dpblNjb3BlIl0sImFtciI6WyJwd2QiXSwiY2xpZW50X2lkIjoiR3B0d19Qb3J0YWwiLCJzdWIiOiIwYTNlM2RmOC1kZGRmLTQ0MDMtYTMzZC0yMGFmODhmYTA5MGEiLCJhdXRoX3RpbWUiOjE2OTc2MzUyNjcsImlkcCI6ImxvY2FsIiwic2lkIjoiOUI4ODY2N0U0RjZDMDBCOEM0NzYzMzBDNUE1RkQzNUIifQ.mhNbQSZ7vT7vqdyt0SKW8vcAXPeCJXpDILt8wUoHos0t6eCEhJySJ0j_1LLQf3DLsHbAiioBdW_gms62Suv0LhHJPDZlIqcr8poVrVVNbS3AYE7rmvmY4VKrRfg3D5lmqlqtIdw2NUO-kpts-vtc321z--7VezQrXyqwqiwNGL16i-1JHA-nY0Cbgwhob-pSelNxyLH00l1HZdJidLUPEdhfTJ8rpmeUimNxOAdxEBpcbtacBt4xnY6Xn__yYuZ7Qs-WeBL8bLTZ1K12S4_m6s24bQKuw2j0NvtIa8n7rl9sXO7HzduZYC80BD-C4SHoQEtW1TuJ2w3JqGPjRvZmBw",
      "token_type": "Bearer",
      "scope": "openid profile all_claims GptwClientLoginScope",
      "profile": {
        "amr": [
          "pwd"
        ],
        "sid": "9B88667E4F6C00B8C476330C5A5FD35B",
        "sub": "0a3e3df8-dddf-4403-a33d-20af88fa090a",
        "auth_time": 1697635267,
        "idp": "local",
        "email": "darcy.patel+5024177@forallcorp.com",
        "given_name": "Darcy",
        "family_name": "Patel",
        "name": "Darcy Patel",
        "gptw_client_id": "5024177",
        "company_name": "TestCo: CB/TI/CA1",
        "role": [
          "ClientAppRole_CMPHRAdmin",
          "ClientAppRole_PortalManager",
          "PortalManagerAchievementNotification"
        ],
        "preferred_username": "darcy.patel+5024177@forallcorp.com"
      },
      "expires_at": expires_at
    };

    console.log(json);

    this.user = User.fromStorageString(JSON.stringify(json));

    this.manager.storeUser(this.user);

    let i = 0;
    let claims: Claim[] = [];
    for (let key in this.user.profile) {
      let value = this.user.profile[key];
      claims[i] = { "claimType": key, "claimValue": value };
      //console.log("completeAuthentication:claimType" + key + ",claimValue" + value )
      i++;
    }

    this.claims = claims;
    this.sessionId = new Date().getTime().toString();

    //if (winRef.nativeWindow.client_config.IsAppMaintenanceMode == 'yes') {
    //  if (!this.isExemptFromMaintanencePage())
    //    this.router.navigate(["/error/downformaintenance"]); //then redirect to the down for maintenance page
    //}

  }

  isLoggedIn(): boolean {
    return this.user != null && !this.user.expired;
  }

  isClaimValid(claimType: string, claimValue: string): boolean {
    console.log("Inside isClaimValid: " + claimType + "ClaimValue: " + claimValue);
    var index: number = this.claims.findIndex(x => x.claimType == claimType);
    if (index >= 0) {
      var cv: any = this.claims[index].claimValue; // this value can be a string OR an array
      if (typeof cv === 'string') {
        if (cv == claimValue)
          return true;
      }
      else {
        for (var i = 0; i < cv.length; i++) {
          if (cv[i] == claimValue)
            return true;
        }
      }
    }
    return false;
  }

  // NOTE: This method can be called for any non role claim because that allows us to simply return a string. Empty or otherwise.
  // The role claim can have multiple roles which would need to return an array. Technically other claims can to.
  // For instance the amr claim. However we're not having to look at that one. Of the claims we need to look out this scheme should work.
  getNonRoleClaim(claimType: string): string {
    if (this.claims != undefined) {
      var index: number = this.claims.findIndex(x => x.claimType == claimType);
      if (index >= 0) {
        return this.claims[index].claimValue;
      }
    }
    return ""
  }

  doesEmployeeHaveExpectedEnvClaim() {
    if (this.claims != undefined) {
      let envClaimName = "GptwAd_" + this.config.ExpectedEnvironmentClaim;
      var index: number = this.claims.findIndex(x => x.claimType == envClaimName);
      if (index >= 0) {
        return this.claims[index].claimValue == this.config.ExpectedEnvironmentClaim;
      }
    }
    return false;
  }

  getClaims(): any {
    return this.claims;
  }

  getSelectedAffiliateId() {
    let returnedValue = "";
    if (localStorage.selectedAffiliateId) {
      returnedValue = localStorage.selectedAffiliateId;
    }
    return returnedValue;
  }

  //getDashboardView() {
  //  let returnedValue = "";
  //  if (localStorage.dashboardView) {
  //    returnedValue = localStorage.dashboardView;
  //  }
  //  return returnedValue;
  //}

  setSelectedAffiliateId(selectedAffiliateId: string) {
    localStorage.selectedAffiliateId = selectedAffiliateId;
  }

  //setDashboardView(dashboardView: string) {
  //  localStorage.dashboardView = dashboardView;
  //}

  isAffiliateIdFoundInClaims(affiliateId: string) {
    return this.getAffiliateClaimCodes().includes(affiliateId);
  }

  getAffilateClaimIdIndex(affiliateId: string) {
    return this.getAffiliateClaimCodes().indexOf(affiliateId);
  }

  getAffiliateClaimCodes(): string[] {
    const result: string[] = [];
    for (let i = 0; i < this.claims.length; i++) {
      const claimType: string = this.claims[i].claimType;
      if (claimType.indexOf("GptwAd_GptwAffiliateId_") == 0) {
        const claimValue: string = this.claims[i].claimValue;
        const str2 = claimValue.substring("GptwAffiliateId_".length);
        result.push(str2);
      }
    }
    result.sort((one, two) => (one > two ? 1 : -1));
    return result;
  }

  hasUSClaim(): boolean {
    const aCodes: string[] = this.getAffiliateClaimCodes();

    if (aCodes.length <= 0) {
      return false;
    }

    const US1Index: number = aCodes.findIndex(x => x === "US1");

    if (US1Index >= 0) {
      return true;
    }

    return false;
  }

  getUS1OrTopAlphaAffiliateCode(): string {
    const result = "";
    const aCodes: string[] = this.getAffiliateClaimCodes();

    if (aCodes.length <= 0) {
      return result;
    }

    const US1Index: number = aCodes.findIndex(x => x == "US1");

    if (US1Index >= 0) {
      return "US1"
    }

    return aCodes[0];
  }

  getAuthorizationHeaderValue(): string {
    return `${this.user.token_type} ${this.user.access_token}`;
  }

  getAuthorizationHeaderTokenValueOnly(): string {
    return `${this.user.access_token}`;
  }

  getUserEmailValue(): string {
    var result = "";
    try {
      result = this.user.profile.email;
    }
    catch { }
    return result;
  }

  getAuthorizationToken(): string {
    return this.user.access_token;
  }

  getAndThenClearZendeskReturnTo(): string {
    let returnedValue: string = "";
    if (localStorage.zendeskReturnTo) {
      returnedValue = localStorage.zendeskReturnTo;
      localStorage.zendeskReturnTo = "";
    }
    return returnedValue;
  }

  getRequestHeaders(): HttpHeaders {
    let headers = new HttpHeaders({ 'Content-Type': 'application/json', 'Authorization': this.getAuthorizationHeaderValue(), 'portal-session-id': this.getSessionId() });
    return headers;
  }

  getSessionId(): string {
    return this.sessionId;
  }

  startAuthentication(): Promise<void> {
    return this.manager.signinRedirect();
  }

  isEmployee() {
    let idpClaim = this.getNonRoleClaim("idp");
    return (idpClaim == 'GptwEmployeeLogin');
  }

  isEmployeeWithPortalClaim(): boolean {
    return this.isClaimValid("GptwAd_GptwAppRole_PortalGeneral", "GptwAppRole_PortalGeneral");
  }

  doesEmployeeHaveDevEnvClaim() {
    if (this.claims != undefined) {
      let envClaimName = "GptwAd_GptwAppEnvironment_DEV";
      var index: number = this.claims.findIndex(x => x.claimType == envClaimName);
      //index = 0;
      if (index >= 0)
        return this.claims[index].claimValue == "GptwAppEnvironment_DEV";
    }
    return false;
  }

  isEndUserWithPortalClaim() {
    console.log("In isEndUserWithPortalClaim Inside the method");
    return this.isClaimValid("role", "ClientAppRole_PortalManager");
    console.log("End of isEndUserWithPortalClaim Inside the method");
  }

  // TBD- If possible  this code should look at the route object and pull this information from there.
  DoesRouteRequirePortalClaim(routePath: string) {
    if (routePath.toLowerCase().indexOf("/helpdesksso") >= 0 ||
      routePath.toLowerCase().indexOf("/error/") >= 0 ||
      routePath.toLowerCase().indexOf("/logout") >= 0)
      return false
    else
      return true
  }

  // Only gptw employees and users with an email address containing @forallcorp.com are exempt
  isExemptFromMaintanencePage() {
    if (!this.isEmployee()) // only continue if we are not an employee
    {
      let email = this.getUserEmailValue();
      console.log("email: " + this.getUserEmailValue());
      if (email != undefined) {
        if (email.toLowerCase().indexOf("@forallcorp.com") > 0) { //check for a @forallcorp.com email
          console.log("forallcorp.com email so skipping the redirect to the Down for maintenance page");
        }
        else {
          return false;
        }
      }
    }
    return true;
  }

  completeAuthentication(): Promise<void> {
    console.log("In CompleteAuthentication")
    return this.manager.signinRedirectCallback().then(user => {
      console.log("Completed SigninRedirectCallback")

      //window.history.replaceState({},
      //  window.document.title,
      //  window.location.origin + window.location.pathname);

      this.user = user;
      let i = 0;
      let claims: Claim[] = [];
      for (let key in this.user.profile) {
        let value = this.user.profile[key];
        claims[i] = { "claimType": key, "claimValue": value };
        //console.log("completeAuthentication:claimType" + key + ",claimValue" + value )
        i++;
      }

      this.claims = claims;
      this.sessionId = new Date().getTime().toString();

      let returnUrl = "";

      if (localStorage.hasOwnProperty('returnUrl')) {
        returnUrl = localStorage.returnUrl;
      }

      console.log("Auth.Service-navigate to:" + returnUrl);

      if (this.config.IsAppMaintenanceMode.toLowerCase() == 'yes') {
        if (!this.isExemptFromMaintanencePage()) {
          this.router.navigate(["/error/downformaintenance"]); //then redirect to the down for maintenance page
          return;
        }
      }

      // Check for token expiration and redirect to isExpired message

      this.timerId = setInterval(() => {
        const isExpired: boolean = user.expired;
        try {
          const hours = Math.floor(user.expires_in / 60 / 60);
          const minutes = Math.floor(user.expires_in / 60) - (hours * 60)
          if ((minutes % 10) == 0)
            console.log("session expires in " + hours + " hours, " + minutes + " minutes");
        } catch { }
        if (isExpired) {
          clearInterval(this.timerId);
          this.router.navigate(["/error/sessionExpired"]);
        }

        try {
          const url = this.baseUrl + 'api/Portal/GetAppConfigDetails?appVersion=' + this.appVersion;
          this.httpClient.get<AppConfigDetailsResult>(url, { headers: this.getRequestHeaders() }).subscribe(result => {
            if (result) {
              if (result.isError) {
                console.log("GetAppConfigDetails returns isError = true,errorStr = " + result.errorStr);
              }
              else {
                console.log("result.appVersion:" + result.appVersion + ",this.appVersion:" + this.appVersion);
                if (this.appVersion != null && result.appVersion != this.appVersion) {
                  console.log("Detected a new app version. Reloading the browser. Current Version:" + this.appVersion + ",New Version:" + result.appVersion);
                  window.location.reload(); // We've detected a new version of the client code so refresh to load the new version
                }
                if (this.appVersion == null && result.appVersion != null) {
                  console.log("this.appVersion changed from null to:" + result.appVersion);
                }
                this.appVersion = result.appVersion;
                if (this.isAppMaintenanceMode != 'yes' && result.isAppMaintenanceMode.toLowerCase() == 'yes') {
                  this.isAppMaintenanceMode = result.isAppMaintenanceMode.toLowerCase();
                  if (!this.isExemptFromMaintanencePage()) {
                    this.router.navigate(["/error/downformaintenance"]); //then redirect to the down for maintenance page
                    return;
                  }
                }
              }
            }
            else {
              console.log("GetAppConfigDetails returns result = undefined");
            }
            return;
          }, error => {
            console.log("GetAppConfigDetails throws an error.");
            return;
          }
          );
        }
        catch {
          console.log("Background timer encountered an unhandled exception when checking AppConfigDetails");
        }

      }, 1000 * 60 * 1);

      // comment
      if (this.DoesRouteRequirePortalClaim(returnUrl) && !this.isEmployeeWithPortalClaim() && !this.isEndUserWithPortalClaim()) {
        this.router.navigate(["/error/noportalclaim"]);
        return;
      }

      if (returnUrl) {
        console.log("CompleteAuthentication-navigate to:" + returnUrl)
        this.router.navigate([returnUrl]);
      }
      else {
        console.log('navigate to the root.""');
        this.router.navigate(["/"]);
      }

    }).catch(function (e) {

      if (e.message != undefined) {
        console.log("completeAuthentication: unhandled exception. e.Error:" + e.message);
        if (e.message.toLowerCase().indexOf("iat is in the future") >= 0) {
          window.location.href = "/error/tokenexpiredatlogin";
          return;
        }
        if (e.message.toLowerCase().indexOf("exp is in the past") >= 0) {
          window.location.href = "/error/tokenexpiredatlogin";
          return;
        }
      }
      console.error(e);
      window.location.href = "/error/general";
    });

  }

  public LoginRedirectHandler = (): Promise<any> => {
    return this.manager.getUser().then((user) => {
      // avoid page refresh errors
      if (user === null || user === undefined) {
        return this.manager.signinRedirectCallback(null);
      }
    });
  }

  public logOut(): Promise<void> {
    this.claims = [];
    return this.manager.signoutRedirect();
  }

}

function _window(): any {
  // return the global native browser window object
  return window;
}

@Injectable()
export class WindowRef {
  get nativeWindow(): any {
    return _window();
  }
}

export function getClientSettings(baseUrl: string, authServerAuthority: string): UserManagerSettings {
  let myUserSettings: UserManagerSettings;

  myUserSettings = {
    authority: authServerAuthority,
    client_id: 'Gptw_Portal',
    redirect_uri: baseUrl + 'auth-callback',
    response_type: "id_token token",
    scope: "openid profile all_claims GptwClientLoginScope",
    filterProtocolClaims: true,
    loadUserInfo: true
    //post_logout_redirect_uri: baseUrl + 'error',
  };
  return myUserSettings;
}

export class CustomHttpParamEncoder implements HttpParameterCodec {
  public encodeKey(key: string): string {
    return encodeURIComponent(key);
  }
  public encodeValue(value: string): string {
    return encodeURIComponent(value);
  }
  public decodeKey(key: string): string {
    return decodeURIComponent(key);
  }
  public decodeValue(value: string): string {
    return decodeURIComponent(value);
  }
}


// Requires Authentication (having a token) AND a portal claim (whether an employee or an end user)
export class PortalClaimRequired {
}
// Requires Authentication (having a token) AND an employee claim (idp = 'GptwEmployeeLogin')
export class GPTWEmployeeOnly {
}
// Requires Authentication (having a token) only. No additional Authorization required.
export class AuthenticationOnly {
}
// Requires Neither Authentication or Authorization. For instance the error page has be to able to tell users that they have no access or that an error occurred.
export class OpenAccess {
}


