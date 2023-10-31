import { Component, Inject, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { BackendService } from '../services/backend.service';
import { HttpClient, HttpParameterCodec } from '@angular/common/http';
import { FilterService, PageChangeEvent } from '@progress/kendo-angular-grid';
import { SetStatusResult, DefaultClientEmailGridSettings, ClientEmail, GetClientEmailDataResult, AffiliateInfo, ReturnAffiliates, GridSettings, GetDashboardDataResult, FavoriteClickedResult } from '../models/misc-models';
import { FilterDescriptor, CompositeFilterDescriptor, process, State } from '@progress/kendo-data-query';

const flatten = filter => {
  const filters = (filter || {}).filters;
  if (filters) {
    return filters.reduce((acc, curr) => acc.concat(curr.filters ? flatten(curr) : [curr]), []);
  }
  return [];
};

@Component({
  selector: 'app-emaildashboard',
  templateUrl: './emaildashboard.component.html',
  styleUrls: ['./emaildashboard.component.scss']
})
export class EmailDashboardComponent implements OnInit {
  private baseUrl: string;
  private httpClient: HttpClient;
  private router: Router;
  private engagementId: number;
  public dataIsLoading = false;
  public selectedValue = "";
  public data: any[] = [];
  public gridSettings = DefaultClientEmailGridSettings;
  public filter: CompositeFilterDescriptor;
  private statusFilter: any[] = [];
  public toggleText = "Hide";
  public show = true;
  public popUpContent: string = "";
  public emailTypeItems: Array<string> = ['Invalid', 'Welcome', 'CompleteCB', 'SetSurveyDates', 'SurveyLaunch', 'SurveyResults', 'ReceivedCertification', 'FailedCertification', 'SendToolkitEmail', 'AutomatedWelcome'];

  constructor(public bs: BackendService, http: HttpClient, private route: ActivatedRoute, @Inject('BASE_URL') baseUrl: string, private authService: AuthService,
    private routerRef: Router) {
    this.baseUrl = baseUrl;
    this.httpClient = http;
    this.router = routerRef;

    route.params.subscribe(params => {
      this.engagementId = params['engagementId'];
    })
  }

  ngOnInit(): void {


    this.getGridData();
  }


  public pageChange({ skip, take }: PageChangeEvent): void {
    console.log("pageChange called");
    this.gridSettings.state.skip = skip;
    this.gridSettings.pageSize = take;
    this.gridSettings.state.take = take;
    this.gridSettings.gridData = process(this.data, this.gridSettings.state);
    this.saveGridSettings();
  }

  public mapGridSettings(gridSettings: GridSettings): GridSettings {
    console.log("mapGridSettings called");
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

  mapColumnIndexToColumnName(columnIndex: number): string {
    console.log("mapColumnIndexToColumnName called");
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

  private mapDateFilter = (descriptor: any) => {
    const filters = descriptor.filters || [];

    filters.forEach(filter => {
      if (filter.filters) {
        this.mapDateFilter(filter);
      } else if (filter.field === 'dateTimeSent' && filter.value) {
        filter.value = new Date(filter.value);
      }
      else if (filter.field === 'dateTimeOpened' && filter.value) {
        filter.value = new Date(filter.value);
      }
    });
  }

  public onCellClick(e: any): void {
    if (e.type === 'contextmenu') {
    }
  }


  public dataStateChange(state: State): void {
    console.log("dataStateChange called");
    this.gridSettings.state = state;
    this.gridSettings.gridData = process(this.data, state);
    this.saveGridSettings();
  }


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

  public onColumnVisibilityChange(e: any): void {
    e.columns.forEach(column => {
      this.gridSettings.columnsConfig.find(col => col.field === column.field).hidden = column.hidden;
    });
    this.saveGridSettings();
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

  private saveGridSettings(): void {
  }

  public loadGridSettings() {
  }

  public viewEmail(dataItem: ClientEmail): void {
    this.bs.viewEmail(dataItem.id);
  }

  public getErrorTitle(dataItem: ClientEmail) {
    let result: string = "";
    if (dataItem.isError) {
      result = dataItem.errorMessage;
    }
    return result;
  }

  public getDate(strCreateDate: string) {
    if (strCreateDate == "")
      return "";
    const createDate: Date = new Date(strCreateDate);
    return (createDate.getMonth() + 1) + "/" + createDate.getDate() + "/" + createDate.getFullYear();
  }

  public getFullDateTime(strCreateDate: string) {
    if (strCreateDate == "")
      return "";
    const createDate: Date = new Date(strCreateDate);
    return createDate.toDateString() + " " + createDate.toTimeString()
  }

  public getFullDateTimeOpened(dataItem: ClientEmail) {
    const createDate: Date = new Date(dataItem.dateTimeOpened);
    if (createDate.getFullYear() <= 1)
      return "";
    let result: string = (createDate.getMonth() + 1) + "/" + createDate.getDate() + "/" + createDate.getFullYear();
    if (dataItem.dateTimeOpenedList.length > 1)
      result += " (" + dataItem.dateTimeOpenedList.length + ")"
    return result;
  }

  public getFullDateTimeOpenedTitle(dataItem: ClientEmail) {
    let result: string = "";
    for (let i = 0; i < dataItem.dateTimeOpenedList.length; i++) {
      const createDate: Date = new Date(dataItem.dateTimeOpenedList[i]);
      if (result.length > 0)
        result += "\n";
      result += createDate.toDateString() + " " + createDate.toTimeString()
    }
    return result;
  }

  public getOpened(dataItem: ClientEmail) {
    return (dataItem.dateTimeOpenedList.length == 0 ? "" : dataItem.dateTimeOpenedList.length.toString());
  }


  public getGridData() {
    this.dataIsLoading = true;
    
    const url: string = this.baseUrl + 'api/Portal/GetClientEmailData?engagementId=' + this.engagementId
    this.httpClient.get<GetClientEmailDataResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.data = result.data;
        this.gridSettings.gridData = process(result.data, this.gridSettings.state);
        this.dataIsLoading = false;
      }
      else {
        this.dataIsLoading = false;
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


  getIconName(statusName: string) {
    try {
      if (statusName == "body") {
        return "email"
      }
    }
    catch (error) {
      console.error(error);
    }

    return "error";
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
