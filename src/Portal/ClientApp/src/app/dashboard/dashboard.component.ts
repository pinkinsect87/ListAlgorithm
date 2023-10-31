import { Component, Inject, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { BackendService } from '../services/backend.service';
import { UploadEvent, RemoveEvent, FileInfo, SelectEvent, FileRestrictions } from '@progress/kendo-angular-upload';
import { HttpClient, HttpParameterCodec, HttpResponse } from '@angular/common/http';
import { FilterService, PageChangeEvent, SelectionEvent } from '@progress/kendo-angular-grid';
import { SetStatusResult, OptOutAbandonSurveyResult, DefaultGridSettings, ECRV2Info, AffiliateInfo, ReturnAffiliates, GridSettings, GetDashboardDataResult, FavoriteClickedResult, DataRequestInfo, DataRequestInfoResult, ReturnCountries, GetCountriesForDataRequestResult, CountryInfo, EcrSearchResult } from '../models/misc-models';
import { FilterDescriptor, CompositeFilterDescriptor, process, State } from '@progress/kendo-data-query';
import { ContextMenuComponent, ContextMenuSelectEvent } from '@progress/kendo-angular-menu';
import { Offset } from "@progress/kendo-angular-popup";

enum RENEWAL_STATUS {
  INVALID = -1,
  TOOEARLY = 0,
  ELIGIBLE = 1,
  RENEWED = 2,
  CHURNED = 3,
  NA = 4
}

const flatten = filter => {
  const filters = (filter || {}).filters;
  if (filters) {
    return filters.reduce((acc, curr) => acc.concat(curr.filters ? flatten(curr) : [curr]), []);
  }
  return [];
};

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})

export class DashboardComponent implements OnInit {
  @ViewChild('greenCheckBoxComponent') greenCheckBoxComponent: ElementRef;
  @ViewChild('yellowCheckBoxComponent') yellowCheckBoxComponent: ElementRef;
  @ViewChild('redCheckBoxComponent') redCheckBoxComponent: ElementRef;
  @ViewChild('searchTextbox') searchTextbox: ElementRef;
  private baseUrl: string;
  private httpClient: HttpClient;
  private router: Router;
  public affiliateId: string;
  public dataIsLoading = false;
  public data: any[] = [];
  public affiliateListItems: Array<string> = [];
  public selectedValue = "";
  public affiliateInfo: AffiliateInfo[];
  public gridSettings = DefaultGridSettings;
  public filter: CompositeFilterDescriptor;
  private statusFilter: any[] = [];
  public defaultGridSettings: GridSettings = DefaultGridSettings;
  public greenCheckBox: boolean = false;
  public yellowCheckBox: boolean = false;
  public redCheckBox: boolean = false;
  public mySelection: string[] = [];
  public isOpenDownload: boolean = false;
  public isDownloadCA: boolean = false;
  public isCAIncludeImages: boolean = false;
  public isDownloadCB: boolean = false;c

  public isOpenDataRequestDialog: boolean = false;

  public isDownloadDataExtract: boolean = false;
  public dataRequestCountries: CountryInfo[] = [];
  public selectedDataRequestCountries: CountryInfo[] = [];

  public searchDialogVisible: boolean = false;
  public searchResult: EcrSearchResult;
  public searchNoMatchDialogVisible: boolean = false;
  public clientId: string;
  //public selectedSearchAffiliateName: string = "";
  //public searchRequestAffiliateNames: Array<string> = [];

  public selectedAffiliate: { affiliateName: string; affiliateId: string };
  public searchRequestAffiliates: Array<{ affiliateName: string; affiliateId: string }> = [
    { affiliateName: "Affiate1", affiliateId: "aff1" },
    { affiliateName: "Affiate1", affiliateId: "aff2" },
    { affiliateName: "Affiate2", affiliateId: "aff3" },
  ];

  public emptyAffiliateArray: Array<{ affiliateName: string; affiliateId: string }> = [
  ];
  public defaultAffiliate: { affiliateName: string; affiliateId: string } = {
    affiliateName: "Select Affiliate",
    affiliateId: null,
  };


  //public viewItems: Array<Item> = [
  //  { text: "All", value: 0 },
  //  { text: "Green Journey Health", value: 1 },
  //  { text: "Yellow Journey Health", value: 2 },
  //  { text: "Red Journey Health", value: 3 },
  //];
  //public defaultItem: { text: string; value: number } = {
  //  text: "Please Select...",
  //  value: null,
  //};

  public tiStatusItems: Array<string> = ['Created', 'Setup in Progress', 'Ready to Launch', 'Survey in Progress', 'Survey Closed', 'Data Transferred', 'Data Loaded', 'Abandoned', 'Opted-Out'];
  public caStatusItems: Array<string> = ['Created', 'In Progress', 'Completed', 'Abandoned', 'Opted-Out'];
  public cbStatusItems: Array<string> = ['Created', 'In Progress', 'Completed', 'Abandoned', 'Opted-Out'];
  public cStatusItems: Array<string> = ['Pending', 'Certified', 'Not Certified'];
  public rStatusItems: Array<string> = ['Requested', 'Success', 'Failure'];
  public lStatusItems: Array<string> = ['Pending', 'Eligible', 'Not Eligible'];
  public contextMenuItems: any[] = [];
  public showContextMenu: boolean = false;
  public showNoActionMessage: boolean = false;
  public showToolsContextMenu: boolean = false;
  public showToolsContextPopup: boolean = false;
  public contextPopupOffset: Offset = { left: 0, top: 0 };
  public toolsContextPopupOffset: Offset = { left: 0, top: 0 };
  public tierItems: Array<string> = ['No TI', 'Assess', 'Analyze', 'Accelerate'];
  public journeystatusItems: Array<string> = ['Invalid', 'Created', 'Logged In', 'Dates Selected', 'Launch Approved', 'Survey Live', 'Survey Closed', 'Complete', 'Incomplete', 'Activated', 'Eligible', 'Renewed', 'Churned'];
  public journeyhealthItems: Array<string> = ['Invalid', 'White', 'Green', 'Yellow', 'Red'];
  public renewalstatusItems: Array<string> = ['Invalid', 'Too Early', 'Eligible', 'Renewed', 'Churned', 'NA'];
  public pendingRenewalstatusItems: Array<string> = ['Pending', 'Renewed', 'Churned'];
  public renewalhealthItems: Array<string> = ['Invalid', 'White', 'Green', 'Yellow', 'Red'];
  public engagementstatusItems: Array<string> = ['Invalid', 'Created', 'Logged In', 'Dates Selected', 'Launch Approved', 'Survey Live', 'Survey Closed', 'Complete', 'Incomplete'];
  public engagementhealthItems: Array<string> = ['Invalid', 'White', 'Green', 'Yellow', 'Red'];
  public searchValue: string = "";
  public uploadBaseSaveUrl: string = "/api/DataRequestFileUpload/SaveFile";
  public uploadSaveUrl: string = "";
  public showSearch: boolean = false;
  public showDataRequest: boolean = false;

  @ViewChild("dropdownlist", { static: true }) public dropdownlist: any;
  @HostListener('document:click', ['$event'])
  documentClick(event: MouseEvent) {
    this.showNoActionMessage = false;
  }

  constructor(public bs: BackendService, private route: ActivatedRoute, private authService: AuthService,
    http: HttpClient, @Inject('BASE_URL') baseUrl: string, private routerRef: Router) {
    this.baseUrl = baseUrl;
    this.httpClient = http;
    this.router = routerRef;

    route.params.subscribe(params => {
      this.affiliateId = params['affiliateId'];
    })
  }

  dataRequestFileRestrictions: FileRestrictions = {
    allowedExtensions: ['xlsx']
  };

  onDataRequestClick() {
    this.isOpenDataRequestDialog = true;
  }

  closeDataRequestDialog() {
    this.isOpenDataRequestDialog = false;
  }

  uploadEventHandler(e: UploadEvent) {
    e.data = {
      //cid: this.clientId,
      affiliateId: this.affiliateId
    };
  }

  successEventHandler(e) {
    let fileName: string = e.response.body.fileName;
    let blobName: string = e.response.body.blobName;

    var dataRequest = new DataRequestInfo;
    dataRequest.affiliateId = this.affiliateId;
    dataRequest.requestorEmail = this.authService.getNonRoleClaim("upn");
    dataRequest.uploadedFileName = blobName + ".xlsx"

    this.isOpenDataRequestDialog = false;

    this.dataIsLoading = true;

    this.SubmitDataRequest(dataRequest)
  }

  public SubmitDataRequest(DataRequestInfo: DataRequestInfo) {

    this.httpClient.post<DataRequestInfoResult>(this.baseUrl + "api/Portal/SubmitDataRequest", DataRequestInfo, { headers: this.authService.getRequestHeaders() })
      .subscribe({
        next: data => {
          this.dataIsLoading = false;
          if (data) {
            if (!data.isError) {
              alert("Your Data Request has been submitted. You can check on its progress on the 'View Data Requests' page.");
            }
            else {
              if (data.errorStr.length > 0) {
                alert(data.errorStr);
              }
              else {
                this.router.navigate(["/error/general"]);
              }
            }
          }
          else
            this.router.navigate(["/error/general"]);
          return;
        },
        error: error => {
          this.dataIsLoading = false;
          this.router.navigate(["/error/general"]);
        }
      });
  }


  ngOnInit() {
    this.bs.clearHeaderCompanyName();

    if(this.authService.doesEmployeeHaveDevEnvClaim()) {
      this.showSearch = true;
      this.showDataRequest = true;
    }

    this.uploadSaveUrl = this.uploadBaseSaveUrl + "?t=" + this.authService.getAuthorizationToken();

    this.loadGridSettings();

    if (this.isJourneyHealthFilterActive("Green")) {
      this.greenCheckBox = true;
    }
    if (this.isJourneyHealthFilterActive("Yellow")) {
      this.yellowCheckBox = true;
    }
    if (this.isJourneyHealthFilterActive("Red")) {
      this.redCheckBox = true;
    }

    //let dashboardView = this.gridSettings.dashboardView;
    //this.gridSettings.dashboardView = dashboardView;

    // If affiliateId is not specified in the URL we need to determine it
    if (this.affiliateId == undefined) { // If we don't find an AffiliateId we're going to either route to an appropriate one or the no access page
      const route = this.setDefaultAffiliateId();
      this.router.navigate([route]);
    }
    else {
      // Check that the AffiliateId we are going to use is actually one of the AffiliateId's that the user has claims for
      if (!this.authService.isAffiliateIdFoundInClaims(this.affiliateId)) {
        const affiliateId = this.authService.getUS1OrTopAlphaAffiliateCode();
        if (affiliateId == "")
          this.router.navigate(["/error/noaccess"]);
        else {
          this.setDefaultAffiliateId();
          this.router.navigate(["/"]);
        }
      }
      else {
        this.getAffiliateData();
        this.getGridData();
      }
    }
    }

  private writeFilters = (str: string, descriptor: any, level: number) => {
    //const filters = descriptor.filters || [];
    //if (level == 0) {
    //  console.log(str);
    //  console.log("writeFilters:start")
    //}
    //filters.forEach(filter => {
    //  if (filter.filters) {
    //    console.log("logic:" + filter.logic)
    //    this.writeFilters(str, filter, (level + 1));
    //  }
    //  else {
    //    console.log("filter[" + level + "] (field,operator,value)=" + filter.field + "," + filter.operator + "," + filter.value);
    //  }
    //});
    //if (level == 0)
    //  console.log("writeFilters:end")
  }


  public mapGridSettings(gridSettings: GridSettings): GridSettings {
    const state = gridSettings.state;
    this.mapDateFilter(state.filter);

    return {
      version: gridSettings.version,
      showMyFavorites: gridSettings.showMyFavorites,
      selectedAffiliateId: gridSettings.selectedAffiliateId,
      state,
      pageSize: gridSettings.pageSize,
      pageSizes: gridSettings.pageSizes,
      columnsConfig: gridSettings.columnsConfig.sort((a, b) => a.orderIndex - b.orderIndex),
      gridData: process(this.data, state)
    };
  }

  private saveGridSettings(): void {
    const gridConfig = {
      version: this.gridSettings.version,
      showMyFavorites: this.gridSettings.showMyFavorites,
      selectedAffiliateId: this.gridSettings.selectedAffiliateId,
      pageSize: this.gridSettings.pageSize,
      pageSizes: this.gridSettings.pageSizes,
      columnsConfig: this.gridSettings.columnsConfig,
      state: this.gridSettings.state
    };

    this.bs.set("DashboardSettings", gridConfig);
    //this.bs.set("GridSettingsNameV36", gridConfig);
    //this.writeFilters("Saved to localstorage:", this.gridSettings.state.filter, 0);
    //this.writeColumnOrderToConsole("Save column order: ");
  }

  public loadGridSettings() {
    const gridSettings: GridSettings = this.bs.get("DashboardSettings");
    let foundVersion: boolean = false;
    try {
      let version: number = gridSettings.version;
      if (version == DefaultGridSettings.version)
        foundVersion = true;
    }
    catch { }

    if (!foundVersion) {
      this.resetGridSettingsToDefault();
      this.saveGridSettings();
    }
    else {
      if (gridSettings !== null) {
        this.gridSettings = this.mapGridSettings(gridSettings);
        this.writeFilters("Loaded from localstorage:", this.gridSettings.state.filter, 0);
      }
      else {
        console.log("No gridSettings loaded from localstorage!")
      }
    }
  }

  public resetGridSettingsToDefault() {
    this.gridSettings.showMyFavorites = this.defaultGridSettings.showMyFavorites;
    let affiliateId = this.authService.getUS1OrTopAlphaAffiliateCode();
    if (affiliateId == "")
      this.router.navigate(["/error/noaccess"]);
    else {
      this.gridSettings.selectedAffiliateId = affiliateId;
    }
    this.gridSettings.state.filter = {
      logic: 'and',
      filters: []
    };
    this.gridSettings.state.sort = [{
      dir: 'desc',
      field: 'createdate'
    }];
    for (let i = this.gridSettings.columnsConfig.length - 1; i >= 0; i--) {
      const fieldName: string = this.gridSettings.columnsConfig[i].field;
      const column: any = this.defaultGridSettings.columnsConfig.find(x => x.field === fieldName);
      this.gridSettings.columnsConfig[i].hidden = column.hidden;
    }
  }


  public resetGridSettingsToDefaultAndSave() {
    this.resetGridSettingsToDefault();
    this.saveGridSettings();
    this.router.navigate(["/"]);
  }

  public setDefaultAffiliateId(): string {
    let resultRoute = "";
    // If one hasn't been selected, pick one, save it and redirect back to this page with the correct AffiliateId
    //let affiliateId = this.authService.getSelectedAffiliateId();

    let affiliateId = this.gridSettings.selectedAffiliateId;

    if (affiliateId === "")
      affiliateId = this.authService.getUS1OrTopAlphaAffiliateCode()
    if (affiliateId === "") {
      resultRoute = "/error/noaccess";
    }
    else {
      this.gridSettings.selectedAffiliateId = affiliateId;
      this.saveGridSettings();
      resultRoute = "employee/dashboard/" + affiliateId;
    }
    return resultRoute;
  }

  public getTitleText(dataItem, statusName, status) {
    //if (statusName == "cstatus" && status == "Certified") {
    //  if (dataItem.certexdate != "")
    //    return status + " (Expires:" + (dataItem.certexdate.getMonth() + 1) + "/" + dataItem.certexdate.getDate() + "/" + dataItem.certexdate.getFullYear() + ")";
    //}
    if (statusName == "cstatus") {
      if (dataItem.allcountrycertification.length > 0) {
        var cstatus = dataItem.allcountrycertification.replaceAll(", ", "\n");
        return cstatus;
      }
      else {
        if (status == "Certified" && dataItem.certexdate != "")
          return status + " (Expires:" + (dataItem.certexdate.getMonth() + 1) + "/" + dataItem.certexdate.getDate() + "/" + dataItem.certexdate.getFullYear() + ")";
      }
    }
    if (statusName == "lstatus") {
      if (dataItem.allcountrylisteligiblity.length > 0) {
        var lstatus = dataItem.allcountrylisteligiblity.replaceAll(", ", "\n");
        return lstatus;
      }
      else {

      }
    }
    return status;
  }

  public showExtraCertIcons(dataItem) {
    try {
      return (this.affiliateId === 'US1' && dataItem.rstatus.toLowerCase() == 'success' && dataItem.rlink != null && dataItem.rlink.length > 0)
    }
    catch (error) {
      console.error(error);
    }
  }

  public getAffiliteMenuName(affiliateId: string) {
    let aff: AffiliateInfo = this.affiliateInfo.find(x => x.affiliateId === affiliateId);
    return aff.affiliateName;
  }

  public refreshGrid() {
    this.getGridData();
  }

  public extractAffiliateIdFromMenuName(menuName: string): string {
    let indexStart = menuName.indexOf("(") + 1;
    let indexEnd = menuName.substring(indexStart).indexOf(")");
    return menuName.substring(indexStart, indexStart + indexEnd)
  }

  public buildAffiliateMenu(affiliates) {
    let userAffiliateIdClaims = this.authService.getAffiliateClaimCodes();

    for (let aff of affiliates) {
      if (userAffiliateIdClaims.includes(aff.affiliateId)) {
        this.affiliateListItems.push(aff.affiliateName);
        if (aff.affiliateId == this.affiliateId)
          this.selectedValue = aff.affiliateName;
      }
    }
  }

  public getAffiliateData() {
    this.httpClient.get<ReturnAffiliates>(this.baseUrl + 'api/Affiliates/GetAllAffiliates', { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.isSuccess) {
          this.affiliateInfo = result.affiliates;
          this.buildAffiliateMenu(result.affiliates);
        }
        else {
          this.router.navigate(["/error/general"]);
        }
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return;
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });
  }

  public getGridData() {
    this.dataIsLoading = true;
    const encoder = new CustomHttpParamEncoder();
    let email = this.authService.getNonRoleClaim("upn");
    const url: string = this.baseUrl + 'api/Portal/GetDashboardData?affiliateId=' + this.affiliateId + '&email=' + encoder.encodeKey(email) + '&showMyFavorites=' + this.gridSettings.showMyFavorites;
    this.httpClient.get<GetDashboardDataResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        for (let i = 0; i < result.ecrV2s.length; i++) {
          result.ecrV2s[i].createdate = new Date(result.ecrV2s[i].createdate);
          if (result.ecrV2s[i].certexdate != "")
            result.ecrV2s[i].certexdate = new Date(result.ecrV2s[i].certexdate);
          if (result.ecrV2s[i].surveyopendate != "")
            result.ecrV2s[i].surveyopendate = new Date(result.ecrV2s[i].surveyopendate);
          if (result.ecrV2s[i].surveyclosedate != "")
            result.ecrV2s[i].surveyclosedate = new Date(result.ecrV2s[i].surveyclosedate);
          //if (result.ecrV2s[i].duration >= 738060)
          //  result.ecrV2s[i].duration = 
        }

        this.data = result.ecrV2s;

        this.gridSettings.gridData = process(result.ecrV2s, this.gridSettings.state);

        this.dataIsLoading = false;
      }
      else {
        this.dataIsLoading = false;
        //this.open('dialog', "GetCompanyUsers failed with error: result of controller call is undefined");
        this.router.navigate(["/error/general"]);
      }
    }, error => {
      if (error.status == 401) {
        this.dataIsLoading = false;
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.dataIsLoading = false;
        this.router.navigate(["/error/general"]);
      }
    });
  }

  public selectionChange(value: any): void {
    let affiliateId = this.extractAffiliateIdFromMenuName(value);
    //this.authService.setSelectedAffiliateId(affiliateId);
    this.gridSettings.selectedAffiliateId = affiliateId;
    this.gridSettings.state.skip = 0;
    this.saveGridSettings();
    // this.getGridData();
    this.router.navigate(["employee/dashboard/"]);
  }

  onSearchKeyDown(e: any) {
    if (e.key === "Enter") {
      this.onSearchClick();
    }
  }

  public closeSearchNoMatchDialog() {
    this.searchNoMatchDialogVisible = false;
  }

  public closeSearchDialog() {
    this.searchDialogVisible = false;
  }

  public handleSearchRequestAffiliateChange(value) {
    this.searchDialogVisible = false;
    this.showAffiliateSearch(value.affiliateId);
  }

  public onSearchClick() {
    this.dataIsLoading = true
    this.searchValue = this.searchValue.trim();
    const encoder = new CustomHttpParamEncoder();
    const url: string = this.baseUrl + 'api/Portal/SearchECRs?value=' + encoder.encodeKey(this.searchValue);
    this.httpClient.get<EcrSearchResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.dataIsLoading = false;
        this.searchResult = result;
        if (result.affiliates.length < 1) {
          this.searchNoMatchDialogVisible = true;
        }
        if (result.affiliates.length == 1) {
          this.showAffiliateSearch(result.affiliates[0].affiliateId);
        }
        if (result.affiliates.length > 1) {
          this.searchRequestAffiliates.splice(0)
          for (let i = 0; i < result.affiliates.length; i++) {
            let aff: { affiliateName: string; affiliateId: string } = {
                affiliateName: result.affiliates[i].affiliateName,
                affiliateId: result.affiliates[i].affiliateId,
            };
            this.searchRequestAffiliates[i] = aff;
          }
          this.searchRequestAffiliates = this.searchRequestAffiliates.sort((a, b) => (a.affiliateName < b.affiliateName) ? -1 : 1);
          //this.selectedAffiliate.affiliateName = result.affiliates[0].affiliateName;
          //this.selectedAffiliate.affiliateId = result.affiliates[0].affiliateId;
         // this.dropdownlist.toggle(true);
          this.searchDialogVisible = true;
        }
      }
      else {
        this.dataIsLoading = false;
        //this.open('dialog', "GetCompanyUsers failed with error: result of controller call is undefined");
        this.router.navigate(["/error/general"]);
      }
    }, error => {
      if (error.status == 401) {
        this.dataIsLoading = false;
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.dataIsLoading = false;
        this.router.navigate(["/error/general"]);
      }
    });
  }

  public showAffiliateSearch(affiliateId:string) {
    let searchResult: EcrSearchResult = this.searchResult;
    let affiliateChanged = false;

    this.gridSettings.state.filter = {
        logic: 'and',
        filters: []
    };
    this.gridSettings.state.sort = [{
        dir: 'desc',
        field: 'createdate'
    }];
    
    //this.saveGridSettings();
    //this.router.navigate(["/"]);
    //this.resetGridSettingsToDefault()

    let filter: CompositeFilterDescriptor = {
      "filters": [

      ],
      "logic": "or"
    }

    if (searchResult.numCNameMatches > 0) {
      filter.filters.push({
        "field": "cname",
        "operator": "contains",
        "value": this.searchValue
      })
      this.gridSettings.state.filter.filters.push(filter);

      for (let i = this.gridSettings.columnsConfig.length - 1; i >= 0; i--) {
        const fieldName: string = this.gridSettings.columnsConfig[i].field;
        if (fieldName == "cname") {
          const column: any = this.defaultGridSettings.columnsConfig.find(x => x.field === fieldName);
          this.gridSettings.columnsConfig[i].hidden = false;
        }
      }

      if (searchResult.affiliates.length > 0) {
        if (affiliateId != this.affiliateId) {
          this.authService.setSelectedAffiliateId(affiliateId);
          this.gridSettings.selectedAffiliateId = affiliateId;
          affiliateChanged = true;
        }
      }

      this.gridSettings.state.skip = 0;
      this.saveGridSettings();
      // this.getGridData();
      if (searchResult.affiliates.length > 0) {
        this.router.navigate(["employee/dashboard/"]);
      }
      else {
        this.refreshGrid();
      }

    }

    if (searchResult.numCIDMatches > 0) {
      filter.filters.push({
        "field": "cid",
        "operator": "eq",
        "value": this.searchValue
      })
      this.gridSettings.state.filter.filters.push(filter);

      for (let i = this.gridSettings.columnsConfig.length - 1; i >= 0; i--) {
        const fieldName: string = this.gridSettings.columnsConfig[i].field;
        if (fieldName == "cid") {
          const column: any = this.defaultGridSettings.columnsConfig.find(x => x.field === fieldName);
          this.gridSettings.columnsConfig[i].hidden = false;
        }
      }

      if (searchResult.affiliates.length > 0) {
        if (affiliateId != this.affiliateId) {
          this.authService.setSelectedAffiliateId(affiliateId);
          this.gridSettings.selectedAffiliateId = affiliateId;
          affiliateChanged = true;
        }
      }

      this.gridSettings.state.skip = 0;
      this.saveGridSettings();
      // this.getGridData();
      if (searchResult.affiliates.length > 0) {
        this.router.navigate(["employee/dashboard/"]);
      }
      else {
        this.refreshGrid();
      }
    }

    if (searchResult.numEIDMatches > 0) {
      filter.filters.push({
        "field": "eid",
        "operator": "eq",
        "value": this.searchValue
      })
      this.gridSettings.state.filter.filters.push(filter);

      for (let i = this.gridSettings.columnsConfig.length - 1; i >= 0; i--) {
        const fieldName: string = this.gridSettings.columnsConfig[i].field;
        if (fieldName == "eid") {
          const column: any = this.defaultGridSettings.columnsConfig.find(x => x.field === fieldName);
          this.gridSettings.columnsConfig[i].hidden = false;
        }
      }

      if (searchResult.affiliates.length > 0) {
        if (affiliateId != this.affiliateId) {
          this.authService.setSelectedAffiliateId(affiliateId);
          this.gridSettings.selectedAffiliateId = affiliateId;
          affiliateChanged = true;
        }
      }

      this.gridSettings.state.skip = 0;
      this.saveGridSettings();
      // this.getGridData();
      if (affiliateChanged) {
        this.router.navigate(["employee/dashboard/"]);
      }
      else {
        this.refreshGrid();
      }
    }
  }

  public isJourneyHealthFilterActive(color: string): boolean {
    let result: boolean = false;

    console.log("isJourneyHealthFilterActive called");

    let filters: any = this.gridSettings.state.filter.filters[0];

    if (filters) {
      for (let i = 0; i < filters.filters.length; i++) {
        let f: any = filters.filters[i];
        if (f.field == "journeyhealth") {
          if (f.value == color) {
            result = true;
          }
        }
      }
    }

    //let filter: CompositeFilterDescriptor = this.gridSettings.state.filter;

    ////filter.filters[0].filters[0]

    //let arrayOfFilters[] = filter.filters[0].filters;

    //for (let i = 0; i < filter.filters[0].filters; i++) {
    //  let f: any = filter.filters;
    //  if (f.field === "journeyhealth" && f.value === color) {
    //    return true;
    //  }
    //}

    //filter.filters.forEach(filter => {
    // if (filter.field === 'createdate' && filter.value) {
    //    filter.value = new Date(filter.value);
    //  }
    //});


    //filters.forEach(filter => {
    //  if (filter.field === 'journeyhealth' && filter.value === '') {
    //  }

    //if (filter.filters) {

    //  this.mapDateFilter(filter);
    //} else if (filter.field === 'createdate' && filter.value) {
    //  filter.value = new Date(filter.value);

    //}
    // });

    //let filter2: CompositeFilterDescriptor = filter.filters[0];

    //for (let i = 0; i < filter2.length; i++) {

    //  let filter2: FilterDescriptor = filter.filters[i];

    //  if (filter.filters[i]   .filters.filters[i].field = "journeyhealth") {
    //  }

    //}

    //"field": "journeyhealth",
    //  "operator": "eq",
    //    "value": "Green"
    //'  .filters.find(x => x.field == "journeyhealth");

    return result;
  }

  //public viewSelectionChange(menuItem: Item): void {
  //  //this.authService.setDashboardView(value);

  //  this.gridSettings.dashboardView = menuItem.value;

  //  this.gridSettings.state.filter.filters = [];

  //  let health = "";

  //  switch (menuItem.value) {
  //    case 1:
  //      health = "Green";
  //      break;
  //    case 2:
  //      health = "Yellow";
  //      break;
  //    case 3:
  //      health = "Red";
  //      break;
  //  }

  //  if (health.length > 0) {
  //    let filter: CompositeFilterDescriptor = {
  //      "filters": [
  //      ],
  //      "logic": "or"
  //    }
  //    this.gridSettings.state.filter.filters.push(filter);
  //    this.gridSettings.state.sort = [{
  //      dir: 'asc',
  //      field: 'createdate'
  //    }];
  //  }
  //  else {
  //    this.gridSettings.state.filter.filters = [];
  //  }

  //  this.saveGridSettings();
  //  this.refreshGrid();
  //}

  public showMyFavoritesValueChangeEvent(value: any): void {
    //this.getGridData();
    let v: boolean = value;
    this.gridSettings.showMyFavorites = v
    this.saveGridSettings();
    this.refreshGrid();
  }

  public toggle(checkBoxName: string): void {
    let greenCheckBox = this.greenCheckBox;
    let yellowCheckBox = this.yellowCheckBox;
    let redCheckBox = this.redCheckBox;

    if (checkBoxName == "Green") {
      this.greenCheckBox = !this.greenCheckBox;
      greenCheckBox = this.greenCheckBox;
    }
    if (checkBoxName == "Yellow") {
      this.yellowCheckBox = !this.yellowCheckBox;
      yellowCheckBox = this.yellowCheckBox;
    }
    if (checkBoxName == "Red") {
      this.redCheckBox = !this.redCheckBox;
      redCheckBox = this.redCheckBox;
    }

    this.gridSettings.state.filter.filters = [];

    if (greenCheckBox || yellowCheckBox || redCheckBox) {
      let filter: CompositeFilterDescriptor = {
        "filters": [

        ],
        "logic": "or"
      }

      if (greenCheckBox)
        filter.filters.push({
          "field": "journeyhealth",
          "operator": "eq",
          "value": "Green"
        })

      if (yellowCheckBox)
        filter.filters.push({
          "field": "journeyhealth",
          "operator": "eq",
          "value": "Yellow"
        })

      if (redCheckBox)
        filter.filters.push({
          "field": "journeyhealth",
          "operator": "eq",
          "value": "Red"
        })

      this.gridSettings.state.filter.filters.push(filter);
      this.gridSettings.state.sort = [{
        dir: 'asc',
        field: 'createdate'
      }];
    }

    this.saveGridSettings();
    this.refreshGrid();
  }

  private randomEl(list) {
    const i = Math.floor(Math.random() * list.length);
    return list[i];
  }

  public getDurationDisplayValue(duration: number) {
    if (duration < 738060) {
      if (duration == 1)
        return duration + " day";
      return duration + " days";
    }

    return "";
  }

  public getDate(strCreateDate: string) {
    if (strCreateDate == "")
      return "";
    const createDate: Date = new Date(strCreateDate);
    return (createDate.getMonth() + 1) + "/" + createDate.getDate() + "/" + createDate.getFullYear();
  }

  public dataStateChange(state: State): void {
    console.log("dataStateChange called");
    let foundGreenCheckbox: boolean = false;
    let foundYellowCheckbox: boolean = false;
    let foundRedCheckbox: boolean = false;

    let filter: CompositeFilterDescriptor = state.filter;

    let filters: any = state.filter.filters[0];

    if (filters) {
      for (let i = 0; i < filters.filters.length; i++) {
        let f: any = filters.filters[i];

        if (f.field == "journeyhealth") {
          if (f.value == "Green") {
            foundGreenCheckbox = true;
          }
          if (f.value == "Yellow") {
            foundYellowCheckbox = true;
          }
          if (f.value == "Red") {
            foundRedCheckbox = true;
          }
        }
      }
    }

    this.greenCheckBox = foundGreenCheckbox
    this.yellowCheckBox = foundYellowCheckbox;
    this.redCheckBox = foundRedCheckbox;

    //this.greenCheckBoxComponent.nativeElement.value = foundGreenCheckbox;
    //this.yellowCheckBoxComponent.nativeElement.value = foundYellowCheckbox;
    //this.redCheckBoxComponent.nativeElement.value = foundRedCheckbox;

    this.gridSettings.state = state;
    this.gridSettings.gridData = process(this.data, state);
    this.saveGridSettings();
  }

  public pageChange({ skip, take }: PageChangeEvent): void {
    this.gridSettings.state.skip = skip;
    this.gridSettings.pageSize = take;
    this.gridSettings.state.take = take;
    this.gridSettings.gridData = process(this.data, this.gridSettings.state);
    this.saveGridSettings();
  }

  public selectedRowChange(selectionEvent: SelectionEvent) {
    if (selectionEvent && selectionEvent.selectedRows) {
      var selected = this.mySelection;
      selectionEvent.selectedRows.forEach(function (e) {


        //if (e.dataItem && (e.dataItem.cbstatus === "Completed" || e.dataItem.castatus === "Completed")) {
          
        //}
        //else {
        //  if (selected && selected.indexOf(e.dataItem.id) > -1) {
        //    const index = selected.indexOf(e.dataItem.id, 0);
        //    if (index > -1) {
        //      selected.splice(index, 1);
        //    }
        //    alert('CA and/or CB not completed.');
        //  }
        //}



      });
    }
  }

  public onReorder(e: any): void {
    let reorderedColumn = this.gridSettings.columnsConfig.splice(e.oldIndex, 1);
    this.gridSettings.columnsConfig.splice(e.newIndex, 0, ...reorderedColumn);
    this.saveGridSettings();
  }

  public onResize(e: any): void {
    e.forEach(item => {
      this.gridSettings.columnsConfig.find(col => col.field === item.column.field).width = item.newWidth;
    });
    this.saveGridSettings();
  }

  public writeColumnOrderToConsole(str: string) {
    //let columns = "";
    //for (let i = 0; i < this.gridSettings.columnsConfig.length; i++) {
    //  if (columns.length > 0)
    //    columns += ","
    //  columns += this.gridSettings.columnsConfig[i].field;
    //  if (this.gridSettings.columnsConfig[i].hidden)
    //    columns += " (hidden)";
    //}
    //console.log(str + columns)
  }

  public onColumnVisibilityChange(e: any): void {
    e.columns.forEach(column => {
      this.gridSettings.columnsConfig.find(col => col.field === column.field).hidden = column.hidden;
    });
    this.saveGridSettings();
  }

  private mapDateFilter = (descriptor: any) => {
    const filters = descriptor.filters || [];

    filters.forEach(filter => {
      if (filter.filters) {
        this.mapDateFilter(filter);
      } else if (filter.field === 'createdate' && filter.value) {
        filter.value = new Date(filter.value);
      }
      else if (filter.field === 'surveyopendate' && filter.value) {
        filter.value = new Date(filter.value);
      }
      else if (filter.field === 'surveyclosedate' && filter.value) {
        filter.value = new Date(filter.value);
      }
      else if (filter.field === 'certexdate' && filter.value) {
        filter.value = new Date(filter.value);
      }
    });
  }

  mapColumnIndexToColumnName(columnIndex: number): string {
    let index = 0;
    for (let i = 0; i < this.gridSettings.columnsConfig.length; i++) {
      if (this.gridSettings.columnsConfig[i].hidden)
        continue;
      if (index === columnIndex)
        return this.gridSettings.columnsConfig[i].field;
      index++;
    }
    return "";
  }

  // cid is passed
  favoriteClick(dataItem) {
    let cid = dataItem;
    const encoder = new CustomHttpParamEncoder();
    let email = this.authService.getNonRoleClaim("upn");
    const url: string = this.baseUrl + 'api/Portal/FavoriteClicked?clientId=' + cid + '&email=' + encoder.encodeKey(email);
    this.httpClient.get<FavoriteClickedResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.isError) {
          //if (this.showMyFavorites)
          this.getGridData();
        }
        else {
          this.router.navigate(["/error/general"]);
        }
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return;
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });

  }

  getIconName(statusName: string, status: string) {
    try {
      if (status !== undefined) {
        if (status == "")
          return "";
        switch (statusName.toLowerCase()) {
          case "tistatus":
            switch (status.toLowerCase()) {
              case "created":
              case "setup in progress":
                return "edit"
                break;
              case "ready to launch":
                return "today"
                break;
              case "survey in progress":
                return "update"
                break;
              case "survey closed":
                return "watch_later"
                break;
              case "data transferred":
                return "sync"
                break;
              case "data loaded":
                return "check_circle_outline"
                break;
              case "abandoned":
                return "delete_forever"
                break;
              case "opted-out":
                return "delete_outline"
                break;
              default:
                return "error"
                break;
            }
            break;
          case "cbstatus":
          case "castatus":
            switch (status.toLowerCase()) {
              case "created":
                return "panorama_fish_eye"
                break;
              case "in progress":
                return "edit"
                break;
              case "completed":
                return "check_circle_outline"
                break;
              case "abandoned":
                return "delete_forever"
                break;
              case "opted-out":
                return "delete_outline"
                break;
              default:
                return "error"
                break;
            }
            break;
          case "cstatus":
            switch (status.toLowerCase()) {
              case "pending":
                return "panorama_fish_eye"
                break;
              case "certified":
                return "check_circle_outline"
                break;
              case "not certified":
                return "highlight_off"
                break;
            }
            break;
          case "rstatus":
            switch (status.toLowerCase()) {
              case "requested":
                return "panorama_fish_eye"
                break;
              case "success":
                return "check_circle_outline"
                break;
              case "failure":
                return "highlight_off"
                break;
            }
            break;
          case "lstatus":
            switch (status.toLowerCase()) {
              case "pending":
                return "panorama_fish_eye"
                break;
              case "eligible":
                return "check_circle_outline"
                break;
              case "not eligible":
                return "highlight_off"
                break;
            }
            break;
          case "tier":
            switch (status.toLowerCase()) {
              case "no ti":
                return ""
                break;
              case "assess":
                return "panorama_fish_eye"
                break;
              case "analyze":
                return "radio_button_checked"
                break;
              case "accelerate":
                return "lens"
                break;
            }
            break;
          case "journeystatus":
            switch (status.toLowerCase()) {
              case "invalid":
                return "error"
                break;
              case "created":
                return "panorama_fish_eye"
                break;
              case "logged in":
                return "account_circle"
                break;
              case "dates selected":
                return "date_range"
                break;
              case "launch approved":
                return "thumb_up_alt"
                break;
              case "survey live":
                return "update"
                break;
              case "survey closed":
                return "watch_later"
                break;
              case "complete":
                return "check_circle_outline"
                break;
              case "incomplete":
                return "remove_circle_outline"
                break;
              case "activated":
                return "account_box"
                break
              case "eligible":
                return "timer"
                break;
              case "renewed":
                return "monetization_on"
                break;
              case "churned":
                return "money_off"
                break;
            }
            break;
          case "renewalstatus":
            switch (status.toLowerCase()) {
              case "invalid":
                return "error"
                break;
              case "too early":
                return "snooze"
                break;
              case "eligible":
                return "timer"
                break;
              case "renewed":
                return "monetization_on"
                break;
              case "churned":
                return "money_off"
                break;
              case "na":
                return "alarm_off"
                break;
            }
            break;
          case "engagementstatus":
            switch (status.toLowerCase()) {
              case "invalid":
                return "error"
                break;
              case "created":
                return "panorama_fish_eye"
                break;
              case "logged in":
                return "account_circle"
                break;
              case "dates selected":
                return "date_range"
                break;
              case "launch approved":
                return "thumb_up_alt"
                break;
              case "survey live":
                return "update"
                break;
              case "survey closed":
                return "watch_later"
                break;
              case "complete":
                return "check_circle_outline"
                break;
              case "incomplete":
                return "remove_circle_outline"
                break;
            }
            break;
          case "journeyhealth":
          case "renewalhealth":
          case "engagementhealth":
            switch (status.toLowerCase()) {
              case "invalid":
                return "error"
                break;
              case "white":
                return "favorite_border"
                break;
              case "green":
                return "favorite"
                break;
              case "yellow":
                return "favorite"
                break;
              case "red":
                return "favorite"
                break;
            }
            break;
        }
      }

    }
    catch (error) {
      console.error(error);
    }

    return "error";
  }

  public StatusSelectionChange(name: string, dataItem: any, value: string): void {
    console.log('selectionChange', value);
    const cid = dataItem.cid;
    const eid = dataItem.eid;

    this.dataIsLoading = true;

    this.httpClient.get<SetStatusResult>(this.baseUrl + 'api/Portal/SetStatus?clientId=' + cid + '&engagementId=' + eid + '&name=' + name + '&value=' + value, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.isError) {
          this.getGridData();
          //this.router.navigate(["employee/dashboard/"]);
        }
        else {
          this.router.navigate(["/error/general"]);
        }
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return;
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });

  }

  navigateToCertAppPage(cid) {
    this.router.navigate(["/certify/client/" + cid]);
  }

  navigateToCreateECR() {
    //this.router.navigate(["/admin/newecr"]);
    this.router.navigate(["/admin/newecr2"]);
  }

  navigateToExtLink(dataItem) {
    this.bs.navigateExt(dataItem);
  }

  navigateToPhotoEditor(cid) {
    this.bs.navigateExt("/manageprofile/view/" + cid);
  }

  navigateToProfile(cid) {
    this.bs.navigateExt(this.bs.config.GPTWWebSiteBaseUrl + "certified-company/" + cid);
  }

  navigateToPortal(eid) {
    this.bs.navigateExt("/certify/engagement/" + eid);
  }

  //  this.httpClient.get<SetStatusResult>(this.baseUrl + 'api/Portal/GetCountriesForDataRequest?eids=' + cid + '&engagementId=' + eid + '&name=' + name + '&value=' + value, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
  //    if (result) {
  //      if (!result.isError) {
  //        this.getGridData();
  //        //this.router.navigate(["employee/dashboard/"]);
  //      }
  //      else {
  //        this.router.navigate(["/error/general"]);
  //      }
  //    }
  //    else {
  //      this.router.navigate(["/error/general"]);
  //    }
  //    return;
  //  }, error => {
  //    if (error.status === 401) {
  //      this.router.navigate(["/error/noaccess"]);
  //    }
  //    else {
  //      this.router.navigate(["/error/general"]);
  //    }
  //  });

  //}

  //fires each time the value is changed— when the component is blurred or the value is cleared
  //through the Clear button(see example).When the value of the component is programmatically changed to ngModel
  //or formControl through its API or form binding, the valueChange event is not triggered
  //because it might cause a mix-up with the built -in valueChange mechanisms of the ngModel or formControl bindings.
  public statusFilters(filter: CompositeFilterDescriptor): FilterDescriptor[] {
    console.log("statusFilters called");
    this.statusFilter.splice(
      0, this.statusFilter.length,
      ...flatten(filter).map(({ value }) => value)
    );

    this.saveGridSettings();
    return this.statusFilter;
  }

  //Used by all Status'
  public statusChange(fieldName: string, values: any[], filterService: FilterService): void {
    console.log("statusChange called.fieldName=" + fieldName + ",values:" + values.join(","));
    filterService.filter({
      filters: values.map(value => ({
        field: fieldName,
        operator: 'eq',
        value
      })),
      logic: 'or'
    });
  }

  @ViewChild('gridmenu')
  public gridContextMenu: ContextMenuComponent;

  private contextData: any;
  private contextItem: string;

  public onCellClick(e: any): void {

    if (e.type === 'contextmenu') {

      // right-click event fired in a grid cell

      this.showContextMenu = false;
      this.showNoActionMessage = false;

      switch (e.column.field) {
        case 'tistatus':
        case 'cbstatus':
        case 'castatus':
          if (e.column.field == "castatus" && String(e.dataItem.castatus).toLowerCase() == "completed") {
            this.showContextMenu = true;
            this.contextItem = 'CA';
            this.contextMenuItems = [{ text: 'Download', attr: { 'context-item': 'CA' }}];
          } else if (e.column.field == "cbstatus" && String(e.dataItem.cbstatus).toLowerCase() == "completed") {
            this.showContextMenu = true;
            this.contextItem = 'CB';
            this.contextMenuItems = [{ text: 'Download', attr: { 'context-item': 'CB' } }];
          }
          // Show the menu only if the engagement is not already one of 'certified', 'pending', or 'not certified',
          // except for the case of a CA which can be abandoned or opted out despite certification status
          else if ((e.dataItem.cstatus != "Certified" && e.dataItem.cstatus != "Not Certified" && e.dataItem.cstatus != "Pending") || e.column.field == "castatus") {
            // And only when appropriate for the specific TI, CB, or CA status
            switch (e.column.field) {
              case 'tistatus':
                if (String(e.dataItem.tistatus).toLowerCase() == "created" || String(e.dataItem.tistatus).toLowerCase() == "setup in progress") {
                  this.showContextMenu = true;
                  this.contextItem = 'TI';
                  this.contextMenuItems = [{ text: 'Opt Out' }, { text: 'Abandon' }];
                }
                break;
              case 'cbstatus':
                if (String(e.dataItem.cbstatus).toLowerCase() == "created" || String(e.dataItem.cbstatus).toLowerCase() == "in progress") {
                  this.showContextMenu = true;
                  this.contextItem = 'CB';
                  this.contextMenuItems = [{ text: 'Opt Out' }, { text: 'Abandon' }];
                }
                break;
              case 'castatus':
                if (String(e.dataItem.castatus).toLowerCase() == "created" || String(e.dataItem.castatus).toLowerCase() == "in progress") {
                  this.showContextMenu = true;
                  this.contextItem = 'CA';
                  this.contextMenuItems = [{ text: 'Opt Out' }, { text: 'Abandon' }];
                } else {
                  this.showNoActionMessage = true;
                }
                break;
            }
          }
          else {
            this.showNoActionMessage = true;
          }
          break;
        case 'tools':
          this.showContextMenu = true;
          this.contextItem = 'Tools';
          this.contextMenuItems = [{ text: 'Emails' }];
          break;
        case 'renewalstatus':
          if (String(e.dataItem.renewalstatus).toLowerCase() == "eligible") {
            this.showContextMenu = true;
            this.contextMenuItems = [{ text: 'Renewed' }, { text: 'Churned' }];
          }
          break;
        default:
        // for all other columns, ignore the context click
      }
    }

    if (e.type === 'click' && e.column.field === 'tools') {

      // left-click event fired in a grid cell

      if (e.column.field === 'tools') {
        this.showContextMenu = true;
        this.contextItem = 'Tools';
        this.contextMenuItems = [{ text: 'Emails' }];
      }
    }

    if (this.showContextMenu == true) {
      // Show the context menu
      const originalEvent = e.originalEvent;
      originalEvent.preventDefault();
      this.contextData = e.dataItem;
      this.gridContextMenu.show({ left: originalEvent.pageX, top: originalEvent.pageY });
    }
    else if (this.showNoActionMessage) {
      // Show the no-action message
      const originalEvent = e.originalEvent;
      originalEvent.preventDefault();
      // Set the top-left corner of the popup to the current cursor position
      this.contextPopupOffset.left = e.originalEvent.pageX;
      this.contextPopupOffset.top = e.originalEvent.pageY;
    } else {
      // Do nothing, let browser show it's own context menu
    }
  }


  public onContextSelect(e: ContextMenuSelectEvent): void {
    // e.index = the selected menu item, starting with 0
    // e.item.text = the text of the selected menu item
    // this.contextItem identifies the item (TI, CB, CA)
    switch (e.item.text) {
      case "Opt Out":
        this.optOutAbandonSurvey(this.contextData.cid, this.contextData.eid, this.contextItem, 'opt out');
        break;
      case "Abandon":
        this.optOutAbandonSurvey(this.contextData.cid, this.contextData.eid, this.contextItem, 'abandon');
        break;
      case "Emails":
        console.log('ToolsSelectionChange', e.item.text);
        this.router.navigate(["/admin/emaildashboard/" + this.contextData.eid]);
        break;
      case "Renewed":
        this.renewedChurned(this.contextData.cid, this.contextData.eid, RENEWAL_STATUS.RENEWED);
        break;
      case "Churned":
        this.renewedChurned(this.contextData.cid, this.contextData.eid, RENEWAL_STATUS.CHURNED);
        break;
      case "Download":
        if (e.item.attr && e.item.attr["context-item"]) {
          this.downloadCultureSurvey(this.contextData.cid, this.contextData.eid, e.item.attr["context-item"], this.contextData.country);
        }
        break;
      default:
        console.error("Context menu item '" + e.item.text + "' is unsupported");
    }
  }

  public optOutAbandonSurvey(cid: number, eid: number, surveyType: string, action: string): void {
    console.log('optOutAbandonSurvey', 'SurveyType = ' + surveyType + ', cid = ' + cid + ', eid = ' + eid + ', action = ' + action);

    this.dataIsLoading = true;

    this.httpClient.get<OptOutAbandonSurveyResult>(this.baseUrl + 'api/Portal/OptOutAbandonSurvey?clientId=' + cid + '&engagementId=' + eid + '&surveyType=' + surveyType + '&actionToTake=' + action, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.isError) {
          this.getGridData();
          //this.router.navigate(["employee/dashboard/"]);
        }
        else {
          this.router.navigate(["/error/general"]);
        }
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return;
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });

  }

  public renewedChurned(cid: number, eid: number, newRenewalStatus: RENEWAL_STATUS): void {
    console.log('SetRenewalStatus', 'cid = ' + cid + ', eid = ' + eid + ', action = ' + newRenewalStatus);

    this.dataIsLoading = true;

    this.httpClient.get<OptOutAbandonSurveyResult>(this.baseUrl + 'api/Portal/SetRenewalStatus?clientId=' + cid + '&engagementId=' + eid + '&newRenewalStatus=' + newRenewalStatus, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.isError) {
          this.getGridData();
        }
        else {
          this.router.navigate(["/error/general"]);
        }
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return;
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });

  }

  public downloadCultureSurvey(cid: number, eid: number, surveyType: string, countryCode: string) {
    console.log('DownloadCultureSurvey', 'cid = ' + cid + ', eid = ' + eid + ', surveyType = ' + surveyType + ', countryCode = ' + countryCode);

    this.dataIsLoading = true;

    var encoder = new CustomHttpParamEncoder();
    let token = encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    var url = "api/CultureSurveyDownload/CreatePackage?clientId=" + cid + "&engagementId=" + eid + "&surveyType=" + surveyType + "&countryCode=" + countryCode + "&token=" + token;

    this.httpClient.get<Blob>(this.baseUrl + url, { observe: 'response', responseType: 'blob' as 'json' }).subscribe(
      (result: HttpResponse<Blob>) => {
      if (result) {
        let filename: string = this.getFileName(result.headers.get('content-disposition'))
        let binaryData = [];
        binaryData.push(result.body);
        let downloadLink = document.createElement('a');
        downloadLink.href = window.URL.createObjectURL(new Blob(binaryData, { type: 'blob' }));
        downloadLink.setAttribute('download', filename);
        document.body.appendChild(downloadLink);
        downloadLink.click();
        this.dataIsLoading = false;
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return;
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    });

  }

  //getFileName(response: HttpResponse<Blob>) {
  getFileName(contentDisposition: string) {
    let filename: string;
    try {
      //const contentDisposition: string = response.headers.get('content-disposition');
      const r = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/
      filename = r.exec(contentDisposition)[1];
      filename = filename.replace(/['"]+/g, '');
    }
    catch (e) {
      filename = 'mydownload.zip'
    }
    return filename
  }
}

interface Item {
  text: string;
  value: number;
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


