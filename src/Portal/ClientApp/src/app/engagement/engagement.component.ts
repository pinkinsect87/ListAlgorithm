import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { HttpClient, HttpHeaders, HttpParameterCodec } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { UserEventName, FindCurrentEngagementInfoResult, GetEngagementInfoResult, EngagementInfo, CurrentCertificationInfo, GetListDeadlineInfoResult } from '../models/misc-models';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-engagement',
  templateUrl: './engagement.component.html',
  styleUrls: ['./engagement.component.scss']
})

export class EngagementComponent implements OnInit {
  private baseUrl: string;
  private httpClient: HttpClient;
  private router: Router;
  private engagementId: number;
  public clientId: string;
  public certificationStatus: string;
  public eInfo: EngagementInfo = new EngagementInfo;
  public curCertInfo: CurrentCertificationInfo = new CurrentCertificationInfo;
  public showResults: boolean = false;
  public listDeadlineDate: string = "";
  public listDeadlineName: string = "";
  public cBStatus: string = "";
  public tIStatus: string = "";
  public cAStatus: string = "";
  public certStartDate: string = "";
  public certExpiryDate: string = "";
  public certExpiryDateHTML: string = "";
  public currentOrRecentlyCertified: boolean = false;
  public empResultsDate: string = "";
  public reportDownloadsDate: string = "";
  public bestCompDeadline: string = "";
  public part1Color: string = "";
  public part2Color: string = "";
  public optionalPartColor: string = "";
  public expiresText: string = "";
  public showCertificationDetails: boolean = false;
  public showCAPanel: boolean = false;
  public showCBPanel: boolean = false;
  public showTIPanel: boolean = false;
  public affiliateId: string = "";
  public isUS: boolean = false;
  public priortrustIndexSSOLink: string = "";
  public isLatestECR: boolean = false;
  public isAbandoned: boolean = false;
  public latestEngagementId: number;
  public infoDialogOpened: boolean = false;
  public infoDialogMessage: string = "";
  public infoDialogTop: number = 100;
  public currentCertificationEngagementId: number = 0;
  public isCurrentCertificationExpired: boolean = false;
  public certExpiresExpiredStatus: string = "";
  public certCurrentPreviousStatus: string = "";
  public isMNCCID: boolean = false;
  public ecrCountries: string = "";
  public countriesCount: number;
  public ecrCreationDate: string = "";

  constructor(public bs: BackendService, private route: ActivatedRoute, private authService: AuthService,
    http: HttpClient, @Inject('BASE_URL') baseUrl: string, private routerRef: Router, public datepipe: DatePipe) {
    this.baseUrl = baseUrl;
    this.httpClient = http;
    this.router = routerRef;

    // needed to navigate from an old engagement to the latest engagement id
    // https://stackoverflow.com/questions/41678356/router-navigate-does-not-call-ngoninit-when-same-page
    this.router.routeReuseStrategy.shouldReuseRoute = function () {
      return false;
    };

    route.params.subscribe(params => {
      this.engagementId = params['engagementId'];
      this.certificationStatus = params['status'];
      this.bs.setEngagementIdAndStatus(this.engagementId.toString(), this.certificationStatus);
    })
  }

  ngOnInit() {
    AppInsights.trackPageView("Engagement component:ngOnInit");

    this.bs.setHeaderDependencies("engagementId", this.engagementId);
    this.getEngagementInfo();

  }

  refreshOnStatusChange() {
    let currentStatus: string = this.certificationStatus;
    this.getEngagementInfo();
    if (this.certificationStatus !== currentStatus) {
      let url = "/certify/engagement/" + this.engagementId;
      this.router.navigate([url]);
    }
  }

  getLatestEngagementId() {
    this.httpClient.get<FindCurrentEngagementInfoResult>(this.baseUrl + 'api/Portal/FindCurrentEngagementInfo?propertyName=clientId&property=' + this.clientId, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.errorOccurred) {
          console.log("error:result.errorMessage" + result.errorMessage);
          if (result.errorId == 3) { // ERROR_ID_FAILED_TO_FIND_ECRV2 = 3 This represents the case where we're showing a bookmarked ECR and there isn't a current ecr to take them to. TFS Task #5415
            //this.router.navigate(["/error/notoolkitaccess"]);
            this.isCurrentCertificationExpired = true;
          }
          else {
            this.router.navigate(["/error/general"]);
          }
        }
        else {
          this.latestEngagementId = result.engagementId;
        }
      }
      else {
        console.log("result undefined")
        this.router.navigate(["/error/general"]);
      }
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    }
    );
  }

  getEngagementInfo() {
    this.httpClient.get<GetEngagementInfoResult>(this.baseUrl + 'api/Portal/GetEngagementInfo?engagementId=' + this.engagementId, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {

        this.ecrCountries = result.eInfo.ecrCountries;
        this.ecrCreationDate = result.eInfo.ecrCreationDate;
        this.countriesCount = result.eInfo.countriesCount;

        this.tIStatus = this.adjustTIStatusForDisplay(result.eInfo.trustIndexStatus);
        this.cBStatus = this.adjustCACBStatusForDisplay(result.eInfo.cultureBriefStatus);
        this.cAStatus = this.adjustCACBStatusForDisplay(result.eInfo.cultureAuditStatus);

        this.isMNCCID = result.eInfo.isMNCCID;

        this.clientId = result.eInfo.clientId;
        this.certStartDate = result.curCertInfo.certificationStartDate;
        this.certExpiryDate = result.curCertInfo.certificationExpiryDate;
        this.certExpiryDateHTML = this.replaceSpacesWithNonBreakingSpaces(this.certExpiryDate);

        // is there a recent certification?
        // recently certified means certification expired within the last two years
        let recentCutoffDate: Date = new Date();
        recentCutoffDate.setFullYear(recentCutoffDate.getFullYear() - 2);
        let lastCertExpiryDate: Date = new Date(result.curCertInfo.certificationExpiryDate);
        if (result.curCertInfo.currentlyCertified || lastCertExpiryDate >= recentCutoffDate)
          this.currentOrRecentlyCertified = true;
        else
          this.currentOrRecentlyCertified = false;

        if (result.curCertInfo.currentlyCertified) {
          this.certCurrentPreviousStatus = "Current";
          this.certExpiresExpiredStatus = "Expires";
        } else {
          this.certCurrentPreviousStatus = "Previous";
          this.certExpiresExpiredStatus = "Expired";
        }
        this.priortrustIndexSSOLink = result.curCertInfo.trustIndexSSOLink;
        this.empResultsDate = result.curCertInfo.empResultsDate;
        this.reportDownloadsDate = result.curCertInfo.reportDownloadsDate;
        this.bestCompDeadline = result.eInfo.bestCompDeadline;
        this.isLatestECR = result.eInfo.isLatestECR;
        this.isAbandoned = result.eInfo.isAbandoned;
        this.curCertInfo = result.curCertInfo;
        this.eInfo = result.eInfo;
        this.affiliateId = result.eInfo.affiliateId;
        let affid = result.eInfo.affiliateId;
        if (result.eInfo.affiliateId === "US1") {
          this.isUS = true;
        }
        else {
          this.isUS = false;
        }
        console.log("IsUS");
        console.log(this.isUS);
        console.log("affiliateId " + result.eInfo.affiliateId);
        console.log("affid " + affid);

        // Bail out if this is an abandoned engagement and there are none more recent
        if (result.eInfo.isAbandoned && result.eInfo.isLatestECR) {
          console.log("latest engagement abandoned")
          this.router.navigate(["/error/noengagement"]);
        }

        this.showCBPanel = false;
        if (result.eInfo.cultureBriefStatus != "Abandoned" && result.eInfo.cultureBriefStatus != "Opted-Out") {
          if (result.eInfo.cultureBriefSSOLink) {
            this.showCBPanel = (result.eInfo.cultureBriefSSOLink.length !== 0)
          }
        }

        this.showCAPanel = false;
        if (result.eInfo.cultureAuditStatus != "Abandoned" && result.eInfo.cultureAuditStatus != "Opted-Out") {
          if (result.eInfo.cultureAuditSSOLink) {
            this.showCAPanel = (result.eInfo.cultureAuditSSOLink.length !== 0)
            //if (this.showCAPanel) {
            //  if (result.eInfo.cultureAuditStatus.toLowerCase() == "InProgress") {
            //    this.showInProgressCAOptionalSub = true;
            //  }
            //}
          }
        }
        
        

        this.showTIPanel = false;
        if (result.eInfo.trustIndexStatus != "Abandoned" && result.eInfo.trustIndexStatus != "Opted-Out") {
          if (result.eInfo.trustIndexSSOLink) {
            this.showTIPanel = (result.eInfo.trustIndexSSOLink.length != 0)
          }
        }

        if (result.eInfo.cultureBriefStatus != undefined && result.eInfo.cultureBriefStatus.toLowerCase() == "completed")
          this.part2Color = "#70AD47";
        else
          this.part2Color = "#E40000";

        if (result.eInfo.cultureAuditStatus != undefined && result.eInfo.cultureAuditStatus.toLowerCase() == "completed")
          this.optionalPartColor = "#70AD47";
        else
          this.optionalPartColor = "#E40000";

        if (result.eInfo.trustIndexStatus != undefined && (result.eInfo.trustIndexStatus.toLowerCase() == "data loaded" || result.eInfo.trustIndexStatus.toLowerCase() == "data transferred"))
          this.part1Color = "#70AD47";
        else
          this.part1Color = "#E40000";

       
        this.certificationStatus = result.eInfo.certificationStatus;
        this.currentCertificationEngagementId = result.curCertInfo.engagementId;

        this.showResults = true;

        this.retrieveListDeadlineInfo();

        // This is needed because Employees don't have a clientId claim and we therefore bring this down from the controller call
        this.clientId = result.eInfo.clientId;

        // we attempt to retrieve the latest engagement id which will ensure isCurrentCertificationExpired is correctly set and we use it to display the red banner
        // also ensures the latest id is sent in the case where we wish to use it for navigation
        this.getLatestEngagementId();

      }
      else {
        console.log("result undefined")
        this.router.navigate(["/error/general"]);
      }
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    }
    );
  }

  replaceSpacesWithNonBreakingSpaces(inputString: string) {
    let outputString: string = inputString.replace(/ /g, "&nbsp;");
    return outputString;
  }

  retrieveListDeadlineInfo() {
    this.httpClient.get<GetListDeadlineInfoResult>(this.baseUrl + 'api/Portal/GetListDeadlineInfo', { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.errorOccurred)
          this.listDeadlineDate = this.datepipe.transform(result.listDeadlineInfo.listDeadlineDate, 'mediumDate');
        this.listDeadlineName = result.listDeadlineInfo.listName;
      }
      else {
        console.log("call to GetListDeadlineInfo failed")
      }
    }, error => {
      console.error(error);
    }
    );
  }

  onClickShowCertificationDetails() {
    this.showCertificationDetails = true;
  }

  onClickHideCertificationDetails() {
    this.showCertificationDetails = false;
  }

  adjustTIStatusForDisplay(status: string) {
    let result: string = "";
    if (status)
      result = status.toUpperCase();
    if (result == "CREATED")
      result = "NOT STARTED";
    if (result == "DATA LOADED" || result == "DATA TRANSFERRED")
      result = "COMPLETED";
    return result;
  }

  adjustCACBStatusForDisplay(status: string) {
    let result: string = "";
    if (status)
      result = status.toUpperCase();
    if (result == "CREATED")
      result = "NOT STARTED";
    return result;
  }

  getBadgeUrl(fileType, quality) {
    const encoder = new CustomHttpParamEncoder();
    var url = "/api/badge/CreateBadge?";
    url += "&clientId=" + this.clientId;
    url += "&engagementId=" + this.engagementId;
    url += "&imageType=" + fileType;
    url += "&quality=" + quality;
    url += "&token=" + encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    return url;
  }

  getCurrentBadgeForInProgressUrl(fileType, quality) {
    const encoder = new CustomHttpParamEncoder();
    var url = "/api/badge/CreateBadge?";
    url += "&clientId=" + this.clientId;
    url += "&engagementId=" + this.currentCertificationEngagementId;
    url += "&imageType=" + fileType;
    url += "&quality=" + quality;
    url += "&token=" + encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    return url;
  }

  public close(component) {
    this.infoDialogOpened = false;
  }

  onClickRoute(routeStr: string) {
    switch (routeStr) {
      case "certToolkit": {
        this.router.navigate(["/certtoolkit/view/" + this.clientId]);
        break;
      }
      case "certProfile": {
        if (this.curCertInfo.profilePublishedLink == undefined || this.curCertInfo.profilePublishedLink == "") {
          this.infoDialogMessage = "Your profile is not yet available. Please try again later.";
          this.infoDialogOpened = true;
        }
        else
          this.bs.navigateExtNewWindow(this.curCertInfo.profilePublishedLink);
        break;
      }
      case "store": {
        this.bs.navigateExtNewWindow(this.bs.config.GPTWStoreUrl);
        break;
      }
      case "reportDownloads": {
        this.router.navigate(['/reports/view/' + this.clientId]);
        break;
      }
      case "users": {
        this.router.navigate(["/users/" + this.clientId]);
        break;
      }
      case "refresh": {
        //this.ngOnInit();
        this.refreshOnStatusChange();
        break;
      }
      case "onboardingVideo": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb/onboardingvideo");
        break;
      }
      case "tourofthePortal": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb/portaltour");
        break;
      }
      case "gettingStartedGuide": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360046650434-Getting-Started-with-Certification-Guide");
        break;
      }
      case "knowledgeBase": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb")
        break;
      }
      case "ti": {
        if (this.eInfo.trustIndexSSOLink == undefined)
          alert("The Trust Index Survey is Not Available");
        else {
          this.bs.postUserEvent(UserEventName.engagementGoToTI);
          this.bs.navigateExt(this.eInfo.trustIndexSSOLink);
          break;
        }
      }
      case "ti1": {
        if (this.priortrustIndexSSOLink == undefined)
          alert("The Trust Index Survey is Not Available");
        else {
          this.bs.postUserEvent(UserEventName.engagementGoToTI);
          this.bs.navigateExt(this.priortrustIndexSSOLink);
          break;
        }
      }
      case "ca": {
        if (this.eInfo.cultureAuditSSOLink == undefined)
          alert("The Culture Audit is Not Available");
        else
          this.bs.navigateExt(this.eInfo.cultureAuditSSOLink);
        break;
      }
      case "cb": {
        if (this.eInfo.cultureBriefSSOLink == undefined)
          alert("The Culture Brief is Not Available");
        else
          this.bs.navigateExt(this.eInfo.cultureBriefSSOLink);
        break;
      }
      case "recognition": {
        this.router.navigate(["/recognition/view/" + this.clientId]);
        break;
      }
      case "listCalendar": {
        this.router.navigate(["/listcalendar/" + this.clientId]);
        break;
      }
      case "latestEngagement": {
        if (this.isCurrentCertificationExpired) // if latest ecr has expired send them to the root and they will be sent to the error page telling them that their subscription has expired
          this.router.navigate(["/certify/client/" + this.clientId]);
        else
          this.router.navigate(["/certify/engagement/" + this.latestEngagementId]);
        break;
      }
    }
  }
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
