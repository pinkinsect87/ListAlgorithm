import { Component, OnInit, AfterViewInit, Inject, ElementRef, ViewChild, ComponentRef, ComponentFactoryResolver, ViewContainerRef } from '@angular/core';
import {
  GetCompanyNameResult, ProductAnswerOption, CreateECRResult, AffiliateInfo, ReturnAffiliates, ReturnCountries,
  CountryInfo, GeneralCallResult, CreateEcrRequest2, SurveyCountry//, IsCultureAuditApplicationInProgressResult
} from '../models/misc-models';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { SurveyCountryComponent } from '../newecr2-components/survey-country/survey-country.component';

@Component({
  selector: 'app-newecr2',
  templateUrl: './newecr2.component.html',
  styleUrls: ['./newecr2.component.scss']
})

export class NewEcrComponent2 implements OnInit, AfterViewInit {
  @ViewChild('clientIdControl') clientIdControl: ElementRef;
  @ViewChild('companyNameControl') companyNameControl: ElementRef;
  @ViewChild('productControl') productControl: ElementRef;
  @ViewChild('pcFirstName') pcFirstName: ElementRef;
  @ViewChild('pcLastName') pcLastName: ElementRef;
  @ViewChild('pcEmail') pcEmail: ElementRef;
  @ViewChild('countryContainer', { read: ViewContainerRef }) container: ViewContainerRef;

  private survey_country_unique_key: number = 0;
  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  private primaryContactEmailAddressReady = false;
  private waitingforEmailVerification = false;
  private verifiedEmail = "";

  public componentsReferences = Array<ComponentRef<SurveyCountryComponent>>();
  public createECRRequestData = new CreateEcrRequest2;
  public affiliates: AffiliateInfo[] = [];
  public myAffiliate: AffiliateInfo;
  public affiliateCountries: CountryInfo[] = [];
  public allCountries: CountryInfo[] = [];
  public surveyCountries: CountryInfo[] = [];
  public dropdownProductSubscriptionOptions: ProductAnswerOption[] = [];
  public selectedProductSubscription: ProductAnswerOption;
  public contactFirstName = "";
  public contactLastName = "";
  public contactEmail = "";
  public isInvalidEmail = false;
  public invalidEmailMessage = "";
  public isAddRowDisabled: boolean = true;
  public applyCertificationInstructions: string = "Each country or region applying for Certification will have a dedicated section on the Culture Brief.";
  public addRowInstructions: string = "This subscription only supports Certification in a single Country/Region.";
  public isClientRangeOk = true;
  public isCompanyNameEntered = true;
  public createEcrResultMessage = "";
  public isWaitingForCreateEcr = false;
  public isCreateEcrError = false;
  public isWaitingForCompanyNameCallToFinish = false;
  public isForceDisableCreateButton = false;
  public message = "";
  public cultureAuditInfo = "";
  public isShowCultureAuditOptions = false;
  public createCultureAudit = "";
  public isShowCultureAuditRequiredError = false;
  public isShowCultureAuditError = false;
  public cultureAuditErrorMessage = "";
  public isShowCertApplicationError = false;
  public certApplicationErrorMessage = "Apply for Certification must be checked for at least one Country/Region.";

  constructor(private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
    private router: Router, public bs: BackendService, private componentFactoryResolver: ComponentFactoryResolver) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;
  }

  ngOnInit() {
    AppInsights.trackPageView("NewECR component:ngOnInit");

    if (!this.authService.isClaimValid("idp", 'GptwEmployeeLogin')) {
      this._router.navigate(["/error/noaccess"]);
    }

    if (!this.authService.isClaimValid("GptwAd_GptwAppRole_PortalGeneral", "GptwAppRole_PortalGeneral")) {
      this._router.navigate(["/error/noaccess"]);
    }

    this.getAllAffiliates();
    this.getAllCountries();
    this.loadProductSubscriptionOptions();
  }

  ngAfterViewInit() {
    this.createSurveyCountryComponent(false);
  }

  getAllAffiliates() {
    let myAffiliates = this.authService.getAffiliateClaimCodes();
    let url: string = this._baseUrl + 'api/Affiliates/GetAllAffiliates';
    this.affiliates = [] as AffiliateInfo[];

    this._httpClient.get<ReturnAffiliates>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.isSuccess) {
          const allAffiliates: AffiliateInfo[] = result.affiliates;
          for (const retaff of allAffiliates) {
            for (let affclaim of myAffiliates) {
              if (affclaim === retaff.affiliateId)
                this.affiliates.push(retaff);
            }
          }
        }
        else {
          this.router.navigate(["/error/general"]);
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

  getAllCountries() {
    let url = this._baseUrl + 'api/Portal/GetAllCountries';

    this._httpClient.get<ReturnCountries>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.allCountries = result.countries;
      }
    });
  }

  getCountriesForAffiliate(affiliateId: string) {
    let url = this._baseUrl + 'api/Affiliates/GetCountriesForAffiliate?myAffiliateId=' + affiliateId;
    this.isWaitingForCreateEcr = true;
    this._httpClient.get<ReturnCountries>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.affiliateCountries = result.countries;
        this.isWaitingForCreateEcr = false;
      }
    });
  }

  loadProductSubscriptionOptions() {
    this.dropdownProductSubscriptionOptions = [];
    let newAnswerOption: ProductAnswerOption = new ProductAnswerOption();
    newAnswerOption.name = "Assess ";
    newAnswerOption.value = "standard";
    this.dropdownProductSubscriptionOptions[0] = newAnswerOption;

    newAnswerOption = new ProductAnswerOption();
    newAnswerOption.name = "Analyze ";
    newAnswerOption.value = "ultratailored";
    this.dropdownProductSubscriptionOptions[1] = newAnswerOption;

    newAnswerOption = new ProductAnswerOption();
    newAnswerOption.name = "Accelerate ";
    newAnswerOption.value = "unlimited";
    this.dropdownProductSubscriptionOptions[2] = newAnswerOption;

    if (this.createECRRequestData.affiliateId === "US1") {
      newAnswerOption = new ProductAnswerOption();
      newAnswerOption.name = "None (No TI will be created)";
      newAnswerOption.value = "none";
      this.dropdownProductSubscriptionOptions[3] = newAnswerOption;
    }
  }

  affiliateSelectionChanged(selectedAffiliate: AffiliateInfo) {
    this.getCountriesForAffiliate(selectedAffiliate.affiliateId);

    this.createECRRequestData.affiliateId = selectedAffiliate.affiliateId;

    this.clientIdControl.nativeElement.value = "";
    this.companyNameControl.nativeElement.value = "";
    this.pcFirstName.nativeElement.value = "";
    this.pcLastName.nativeElement.value = "";
    this.pcEmail.nativeElement.value = "";
    this.productControl = null;
    this.selectedProductSubscription = undefined;
    this.resetSurveyCountryComponent();
  }

  addSurveyCountryRow() {
    this.createSurveyCountryComponent();
  }

  createSurveyCountryComponent(isShowRemove: boolean = true) {
    // create the component factory  
    const dynamicComponentFactory = this.componentFactoryResolver.resolveComponentFactory(SurveyCountryComponent);
    // add the component to the view  
    const childComponentRef = this.container.createComponent(dynamicComponentFactory);

    let childComponent = childComponentRef.instance;
    childComponent.unique_key = ++this.survey_country_unique_key;
    childComponent.parentRef = this;
    childComponent.countries = this.surveyCountries;
    childComponent.isShowRemove = isShowRemove;

    // add reference for newly created component
    this.componentsReferences.push(childComponentRef);
  }

  removeSurveyCountryComponent(key: number) {
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

  resetSurveyCountryComponent() {
    if (this.componentsReferences && this.componentsReferences.length > 0) {
      for (const component of this.componentsReferences) {
        this.removeSurveyCountryComponent(component.instance.unique_key);
      }

      this.createSurveyCountryComponent(false);
    }
  }

  productSubscriptionSelected(selectedProduct: ProductAnswerOption) {
    this.resetSurveyCountryComponent();

    if (selectedProduct.name.trim() === "Assess" || selectedProduct.name.trim() === "Analyze" ||
      selectedProduct.name.trim() === "None (No TI will be created)") {
      this.isAddRowDisabled = true;

      this.surveyCountries = this.affiliateCountries;
      if (this.componentsReferences && this.componentsReferences.length > 0) {
        const component = this.componentsReferences[0].instance;
        component.isApplyForCertificationDisabled = true;
        component.isApplyForCertification = true;
        component.countries = this.surveyCountries;
      }

      this.cultureAuditInfo = "A Culture Audit will be created with this Engagement if the minimum employee count is met.";
      this.isShowCultureAuditOptions = false;
      this.isShowCultureAuditRequiredError = false;
      this.isShowCultureAuditError = false;
    }
    else {
      // Accelerate
      this.isAddRowDisabled = false;

      this.surveyCountries = this.allCountries;
      if (this.componentsReferences && this.componentsReferences.length > 0) {
        const component = this.componentsReferences[0].instance;
        component.isApplyForCertificationDisabled = false;
        component.isApplyForCertification = false;
        component.countries = this.surveyCountries;
      }

      this.cultureAuditInfo = "You should only create one new Culture Audit per global project cycle.";
      this.isShowCultureAuditOptions = true;

      if (this.createCultureAudit === undefined ||
        this.createCultureAudit === null ||
        this.createCultureAudit === "") {
        this.isShowCultureAuditRequiredError = true;
      }
      else {
        this.isShowCultureAuditRequiredError = false;

        if (this.createCultureAudit === "Yes" && this.cultureAuditErrorMessage != "") {
          this.isShowCultureAuditError = true;
        }
      }
    }
  }

  verifyClientId() {
    this.isClientRangeOk = false;
    this.message = "Client number ranges for this customer have not been configured";
    if (!this.isNumeric(this.createECRRequestData.clientId))
      this.message = "Client id needs to be numeric";
    else {
      if ((this.createECRRequestData.clientId >= this.myAffiliate.startClientId) && (this.createECRRequestData.clientId <= this.myAffiliate.endClientId))
        this.isClientRangeOk = true;
      else
        this.message = "Client id is outside expected range of " + this.myAffiliate.startClientId + " and " + this.myAffiliate.endClientId + " for " + this.myAffiliate.affiliateName;
    }
    if (this.isClientRangeOk === false) { }
    else {
      this.getCompanyName();
    }
  }

  getCompanyName() {
    let companyNameLookedUp = false;
    // if we haven't yet looked up the companyName and we have a clientId (!= 0) then look it up
    if (this.createECRRequestData.affiliateId === 'US1') {
      if (!companyNameLookedUp && this.createECRRequestData.clientId !== undefined) {
        companyNameLookedUp = true;
        this.isWaitingForCompanyNameCallToFinish = true;
        //sessionStorage.createECRClientId = this.createECRRequestData.clientId;
        let url = this._baseUrl + 'api/Portal/GetCompanyName?clientId=' + this.createECRRequestData.clientId + '&affiliateId=' + this.createECRRequestData.affiliateId;
        this._httpClient.get<GetCompanyNameResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
          this.isWaitingForCompanyNameCallToFinish = false;
          if (result) {
            if (result.errorOccurred) {
              console.error("newECR.component:GetCompanyName returned an error. message:" + result.errorMessage);
              this.createECRRequestData.clientName = "Not Found!"
            }
            else {
              this.createECRRequestData.clientName = result.companyName;
            }
          }
          else {
            this.createECRRequestData.clientName = "Not Found!"
            console.error("newecr.component:GetCompanyName returned a result of 'undefined'")
          }
        }, error => {
          this.isWaitingForCompanyNameCallToFinish = false;
          this.createECRRequestData.clientName = "Not Found!"
          console.error("newecr.component:GetCompanyName unhandled error:");
        }
        );
      }
    }
  }

  verifyClientName() {
    this.isCompanyNameEntered = true;
    if (this.createECRRequestData.clientName.length == 0)
      this.isCompanyNameEntered = false;
  }

  async validateEmail() {
    if (this.verifiedEmail === this.contactEmail)
      return;
    this.waitingforEmailVerification = true;
    var url = this._baseUrl + 'api/Portal/IsValidEmail?email=' + encodeURIComponent(this.contactEmail);
    await this._httpClient.get<boolean>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result === true || result === false) {
        this.isInvalidEmail = !result;
        this.primaryContactEmailAddressReady = true;
        this.waitingforEmailVerification = false;
        this.verifiedEmail = this.contactEmail;
        if (result === false)
          this.invalidEmailMessage = "Invalid email. Use only A-Z 0-9 ~!$%^&*_=+}{'?-.@";
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

  shouldCreateButtonBeDisabled() {
    let clientIdReady = false;
    let clientNameReady = false;
    let totalEmployeesReady = true;
    let countryReady = true;
    let productTypeReady = false;
    let primaryContactFirstNameReady = false;
    let primaryContactLastNameReady = false;
    let surveyCountriesReady = false;
    let cultureAuditReady = false;

    if (this.createECRRequestData.clientId !== undefined && this.isClientRangeOk === true) {
      clientIdReady = true;
    }

    if (this.createECRRequestData.clientName !== undefined && this.createECRRequestData.clientName.length > 0) {
      clientNameReady = true;
    }

    if (this.selectedProductSubscription !== undefined) {
      this.createECRRequestData.trustIndexSurveyType = this.selectedProductSubscription.value;
      productTypeReady = true;
      if (this.createECRRequestData.countryCode === undefined)
        countryReady = false;
      if (this.createECRRequestData.totalEmployees === undefined || this.createECRRequestData.totalEmployees === 0)
        totalEmployeesReady = false;
    }

    if (this.contactFirstName !== undefined && this.contactFirstName.length > 0) {
      primaryContactFirstNameReady = true;
    }

    if (this.contactLastName !== undefined && this.contactLastName.length > 0) {
      primaryContactLastNameReady = true;
    }
    if (this.contactEmail !== undefined && this.contactEmail.length > 0 && (this.contactEmail.lastIndexOf("@") > 0) && (this.contactEmail.lastIndexOf("@") + 1 < this.contactEmail.length) &&
      (this.contactEmail.lastIndexOf(".") > 0) && (this.contactEmail.lastIndexOf(".") + 1 < this.contactEmail.length)) {
      this.primaryContactEmailAddressReady = true;

    }
    if (this.isInvalidEmail)
      return true;

    // validate survey countries
    if (this.componentsReferences && this.componentsReferences.length > 0) {
      if (this.componentsReferences.length === 1 && this.componentsReferences[0].instance.country === undefined) {
        surveyCountriesReady = false;
      }
      else {
        var components = this.componentsReferences.filter(p => p.instance.isEmployeeCountInvalid || p.instance.isCountryInvalid);
        if (components !== undefined && components !== null && components.length > 0) {
          surveyCountriesReady = false;
        }
        else {
          var applyForCertComponents = this.componentsReferences.filter(p => p.instance.isApplyForCertification);
          if (this.selectedProductSubscription !== undefined &&
            this.selectedProductSubscription !== null &&
            this.selectedProductSubscription.name.trim() === "Accelerate" &&
            (applyForCertComponents === undefined || applyForCertComponents === null ||
              applyForCertComponents.length === 0)) {
            surveyCountriesReady = false;
            this.isShowCertApplicationError = true;
          }
          else {
            surveyCountriesReady = true;
            this.isShowCertApplicationError = false;
          }
        }
      }
    }
    else {
      surveyCountriesReady = false;
    }

    //validate culture audit
    if (this.selectedProductSubscription === undefined || (this.selectedProductSubscription.name.trim() === "Accelerate" && this.createCultureAudit == "")) {
      cultureAuditReady = false;
    }
    else {
      cultureAuditReady = true;
    }

    return (clientIdReady === false || clientNameReady === false || totalEmployeesReady === false || productTypeReady === false || countryReady === false ||
      primaryContactFirstNameReady === false || primaryContactLastNameReady === false || this.primaryContactEmailAddressReady === false ||
      surveyCountriesReady === false || cultureAuditReady === false);
  }

  isSurveyCountriesValid() {
    let isValid = true;

    if (this.componentsReferences && this.componentsReferences.length > 0) {
      var component = this.componentsReferences.find(p => p.instance.isEmployeeCountInvalid || p.instance.isCountryInvalid);
      if (component !== undefined && component !== null) {
        isValid = false;
      }
    }

    return isValid;
  }

  createEngagement() {
    this.isShowCultureAuditError = false;
    this.isCreateEcrError = false;
    if (this.isWaitingForCreateEcr)
      return;

    //this.validateEmail();
    this.bs.saveNewECRResult("");

    if (this.waitingforEmailVerification)
      return;
    if (this.isInvalidEmail)
      return;
    if (!this.isSurveyCountriesValid())
      return;
    
    // add portal contact to the request
    this.createECRRequestData.email = this.contactEmail;
    this.createECRRequestData.firstName = this.contactFirstName;
    this.createECRRequestData.lastName = this.contactLastName;
    this.createECRRequestData.achievementNotification = true;

    this.createECRRequestData.countryCode = this.myAffiliate.defaultCountryCode;

    this.createECRRequestData.surveyCountries = [];
    // add survey countries to the request
    if (this.componentsReferences && this.componentsReferences.length > 0) {
      for (let component of this.componentsReferences) {
        const surveyCountry: SurveyCountry = {
          countryCode: component.instance.country.countryCode,
          isApplyForCertification: component.instance.isApplyForCertification,
          totalEmployees: component.instance.employeeCount
        };

        this.createECRRequestData.surveyCountries.push(surveyCountry);
      }
    }

    if (this.selectedProductSubscription.name.trim() === "Accelerate" && this.createCultureAudit === "Yes") {
      this.createECRRequestData.isCreateCA = true;
    }
    else {
      this.createECRRequestData.isCreateCA = false;
    }

    this.isForceDisableCreateButton = true;
    this.isWaitingForCreateEcr = true;

    this._httpClient.post<CreateECRResult>(this._baseUrl + 'api/Portal/CreateECR2', this.createECRRequestData, { headers: this.authService.getRequestHeaders() }).subscribe(result => {

      this.isCreateEcrError = false;
      this.createEcrResultMessage = "";
      this.isWaitingForCreateEcr = false;
      this.isForceDisableCreateButton = false;
      this.verifiedEmail = "";

      if (result) {
        if (result.isError) {
          if (result.errorId === 999) {
            // CA error
            this.cultureAuditErrorMessage = result.errorStr;
            this.isShowCultureAuditError = true;
          }
          else {
            var err = "Error:" + result.errorStr;
            console.error(err);
            this.isCreateEcrError = true;
            this.createEcrResultMessage = err;
          }
        }
        else {
          this.isCreateEcrError = false;
          var message = "Your Engagement was successfully created.";
          if (result.warningStr.length > 0)
            message += " Warning:" + result.warningStr;
          console.log(message);
          this.bs.saveNewECRResult(message);
          this.bs.saveNewECREngagement(result.engagementId);
          this.router.navigate(["/admin/newecr/done"]);
        }
      }
      else {
        var err = "CreateECR failed with error: result of controller call is undefined";
        console.error(err);
        this.isCreateEcrError = true;
        this.createEcrResultMessage = err;
      }
    }, error => {
      this.isWaitingForCreateEcr = false;
      this.isForceDisableCreateButton = false;
      var err = "CreateECR failed with an unhandled error.";
      console.error(err);
      this.isCreateEcrError = true;
      this.createEcrResultMessage = err;
    });
  }

  isNumeric(val: any): val is number | string {
    return (val - parseFloat(val) + 1) >= 0;
  }
}


