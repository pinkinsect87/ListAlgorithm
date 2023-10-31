import { Component, OnInit, AfterViewInit, Inject, ElementRef, ViewChild, ViewContainerRef, ChangeDetectorRef, ComponentRef, ComponentFactoryResolver } from '@angular/core';
import { ReturnCountries, CountryInfo, DataExtractRequestData, DataExtractCompanyInfo, DataRequestInfoResult } from '../models/misc-models';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { DataExtractRequestCompanyComponent } from '../dataextractrequest-components/dataextractrequest-company/dataextractrequest-company.component';

@Component({
  selector: 'app-dataextractrequest',
  templateUrl: './dataextractrequest.component.html',
  styleUrls: ['./dataextractrequest.component.scss']
})

export class DataExtractRequestComponent implements OnInit, AfterViewInit {
  @ViewChild('fileNameControl') fileNameControl: ElementRef;
  @ViewChild('countryDataIdControl') countryDataIdControl: ElementRef;
  @ViewChild('certificationExpiry') certificationExpiryControl: ElementRef;
  @ViewChild('minEmployees') minEmployeesControl: ElementRef;
  @ViewChild('maxEmployees') maxEmployeesControl: ElementRef;
  @ViewChild('companyContainer', { read: ViewContainerRef }) container: ViewContainerRef;

  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  private company_unique_key: number = 0;

  public componentsReferences = Array<ComponentRef<DataExtractRequestCompanyComponent>>();
  public allCountries: CountryInfo[] = [];
  public yesNoData: string[] = ["Yes", "No"];
  public cultureAuditEssaysData: string[] = ["Yes - Culture Audit Forms Only", "Yes - Culture Audit Forms AND Supplemental Materials", "No"];
  public companyData: string[] = ["Certification Date", "List of Companies"];
  public completedCultureAuditData: string[] = ["Required", "Not Required"];
  public industryData: string[] = ["ALL", "Advertising & Marketing", "Aerospace", "Aging Services", "Agriculture", "Architecture & Design", "Biotechnology & Pharmaceuticals", "Construction", "Education & Training", "Electronics", "Engineering", "Entertainment", "Financial Services & Insurance", "Health Care", "Hospitality", "Industrial Services", "Information Technology", "Manufacturing & Production", "Media", "Mining & Quarrying", "Professional Services", "Real Estate", "Retail", "Social Services and Government Agencies", "Telecommunications", "Transportation", "Other"];
  public industryVerticalData: string[] = ["ALL", "Advertising", "Direct Marketing", "Senior Housing & Care", "At-Home Care", "Biotechnology", "Pharmaceuticals", "Performing Arts & Spectator Sports", "Museums", "Amusement & Gambling", "Investments", "Accounting", "Banking/Credit Services", "Health Insurance", "General Insurance", "Life Insurance", "Auto Insurance", "Home Insurance", "Re-Insurance", "Hospital", "Medical sales/distribution", "Specialty", "Services", "Food and Beverage Service", "Hotel/Resort", "Management", "Vehicle Repair & Maintenance", "Industrial Design", "Waste/Refuse/Recycling Management", "Engineering", "Hardware", "Internet Service Provider", "Software", "Online Internet Services", "IT Consulting", "Storage/Data Management", "Alternative Energy", "Automotive", "Basic metals and fabricated metal products", "Building Materials", "Chemicals", "Coke, refined petroleum products and nuclear fuel", "Electrical and optical equipment", "Electronics", "Energy Distribution", "Food products", "beverages and tobacco", "Furniture", "Gasoline/Retail Marketer", "Healthcare", "Leather and leather products", "Machinery and equipment", "Medical devices", "Non-metallic mineral products", "Personal and household goods", "Pulp, paper and paper products", "Rubber and plastic products", "Safety", "Textiles and textile products", "Transport equipment", "Water supply and treatment", "Wood and wood products", "Publishing and printing", "Television/Film/Video", "Radio", "Online Internet Services", "Architecture/Design", "Consulting-Actuarial/Risk Assessment", "Consulting Engineering", "Consulting Environmental", "Consulting – Management", "Consulting Manufacturing", "Legal", "Security", "Staffing & Recruitment", "Telephone Support/Sales Centers", "Travel Management", "Clothing", "Computers/Electronics", "Food/grocery", "Specialty", "Culture & Arts", "Education", "Housing", "Business Services", "Residential Care", "Airline/Commercial Aviation", "Package Transport", "Transport & Storage", "Amusement and Gambling", "Home health and hospice", "Non-medical home care", "Medicare Advantage plan-providers", "Rehab/therapy at home agencies", "Senior Nursing at Home Services", "Senior Apartments/ Communities", "Skilled Nursing home", "Rehabilitation home", "Alzheimer home (either For Profit/Non-Profit)", "Senior meal providers (either For Profit/Non-Profit)", "Senior Community Centers (government)", "Senior Conservancy/Guardian/Trustee Corporations", "Any Senior focused business regardless of type, retail, etc.", "Late Life Wealth Management", "At-Home Care (including Hospice)", "Senior Housing & Care", "Other"];
  public selectedCountry: CountryInfo;
  public countryDataId: string;
  public fileName: string;
  public selectedTrustIndexData: string;
  public selectedCultureBriefDatapoints: string;
  public selectedCultureAuditEssays: string;
  public selectedPhotosCaptions: string;
  public selectedCompanyData: string;
  public certificationExpiryDate: string;
  public selectedCompletedCultureAuditData: string;
  public selectedIndustry1Data: string;
  public selectedIndustryVertical1Data: string;
  public selectedIndustry2Data: string;
  public selectedIndustryVertical2Data: string;
  public minEmployeesCount: string;
  public maxEmployeesCount: string;
  
  constructor(private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
    private router: Router, public bs: BackendService,
    private componentFactoryResolver: ComponentFactoryResolver) {
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

    this.getAllCountries();
  }

  ngAfterViewInit() { }

  getAllCountries() {
    let url = this._baseUrl + 'api/Portal/GetAllCountries';

    this._httpClient.get<ReturnCountries>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.allCountries = result.countries;
      }
    });
  }

  createCompanyComponent(isShowRemove: boolean = true) {
    // create the component factory  
    const dynamicComponentFactory = this.componentFactoryResolver.resolveComponentFactory(DataExtractRequestCompanyComponent);
    // add the component to the view  
    const childComponentRef = this.container.createComponent(dynamicComponentFactory);

    let childComponent = childComponentRef.instance;
    childComponent.unique_key = ++this.company_unique_key;
    childComponent.parentRef = this;
    childComponent.isShowRemove = isShowRemove;

    // add reference for newly created component
    this.componentsReferences.push(childComponentRef);
  }

  removeCompanyComponent(key: number) {
    if (this.container.length < 1) return;

    let componentRef = this.componentsReferences.find(
      x => x.instance.unique_key == key
    );

    let vcrIndex: number = this.container.indexOf(componentRef.hostView);

    // removing component from container
    this.container.remove(vcrIndex);

    // removing component from the list
    this.componentsReferences = this.componentsReferences.filter(
      x => x.instance.unique_key !== key
    );
  }

  resetCompanyComponent() {
    if (this.componentsReferences && this.componentsReferences.length > 0) {
      for (const component of this.componentsReferences) {
        this.removeCompanyComponent(component.instance.unique_key);
      }
    }
  }

  countrySelectionChanged(country: CountryInfo) {
    this.countryDataId = country.countryCode;
  }

  companyDataSelectionChanged(selectedCompanyInfo: string) {
    this.resetCompanyComponent();
    if (selectedCompanyInfo === 'List of Companies') {
      this.createCompanyComponent(false);
    }
  }

  shouldSubmitButtonBeDisabled() {
    if (this.fileName === undefined || this.fileName === null || this.fileName === "")
      return true;
    if (this.countryDataId === undefined || this.countryDataId === null || this.countryDataId === "")
      return true;
    if (this.selectedTrustIndexData === undefined || this.selectedTrustIndexData === null || this.selectedTrustIndexData === "")
      return true;
    if (this.selectedCultureBriefDatapoints === undefined || this.selectedCultureBriefDatapoints === null || this.selectedCultureBriefDatapoints === "")
      return true;
    if (this.selectedCultureAuditEssays === undefined || this.selectedCultureAuditEssays === null || this.selectedCultureAuditEssays === "")
      return true;
    if (this.selectedPhotosCaptions === undefined || this.selectedPhotosCaptions === null || this.selectedPhotosCaptions === "")
      return true;
    if (this.selectedCompanyData === undefined || this.selectedCompanyData === null || this.selectedCompanyData === "")
      return true;

    if (this.selectedCompanyData === 'Certification Date' && (this.certificationExpiryDate === undefined ||
      this.certificationExpiryDate === null || this.certificationExpiryDate === ""))
      return true;

    if (this.selectedCompanyData === 'List of Companies') {
      var components = this.componentsReferences.filter(p => !p.instance.isValid);
      if (components != null && components.length > 0)
        return true;
    }
  }

  submitDataExtractRequest() {
    var dataExtractRequestData = new DataExtractRequestData;
    dataExtractRequestData.requestorEmail = this.authService.getNonRoleClaim("upn");
    dataExtractRequestData.fileName = this.fileName;
    dataExtractRequestData.countryCode = this.countryDataId;
    dataExtractRequestData.trustIndexData = this.selectedTrustIndexData;
    dataExtractRequestData.cultureBriefDatapoints = this.selectedCultureBriefDatapoints;
    dataExtractRequestData.cultureAuditEssays = this.selectedCultureAuditEssays;
    dataExtractRequestData.photosAndCaptions = this.selectedPhotosCaptions;

    if (this.selectedCompanyData === 'Certification Date') {
      dataExtractRequestData.certificationExpiry = this.certificationExpiryDate;
      dataExtractRequestData.completedCultureAudit = this.selectedCompletedCultureAuditData;
      dataExtractRequestData.industry = this.selectedIndustry1Data;
      dataExtractRequestData.industryVertical = this.selectedIndustryVertical1Data;
      dataExtractRequestData.industry2 = this.selectedIndustry2Data;
      dataExtractRequestData.industryVertical2 = this.selectedIndustryVertical2Data;
      dataExtractRequestData.minimumNumberEmployees = this.minEmployeesCount;
      dataExtractRequestData.maximumNumberEmployees = this.maxEmployeesCount;
    }
    else {
      // add companies to the request
      if (this.componentsReferences && this.componentsReferences.length > 0) {
        for (let component of this.componentsReferences) {
          const company: DataExtractCompanyInfo = {
            clientId: component.instance.clientId,
            engagementId: component.instance.engagement.engagementId,
            clientName: component.instance.companyName
          };

          dataExtractRequestData.dataExtractCompanyInfos.push(company);
        }
      }
    }

    this._httpClient.post<DataRequestInfoResult>(this._baseUrl + "api/Portal/SubmitDataExtractRequest",
      dataExtractRequestData, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.isError) {
          alert("Your Data Request has been submitted. You can check on its progress on the 'View Data Requests' page.");
          this._router.navigate(["/employee/dashboard/"]);
        }
        else {
          if (result.errorStr.length > 0) {
            alert(result.errorStr);
          }
          else {
            this._router.navigate(["/error/general"]);
          }
        }
      } else {
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
}


