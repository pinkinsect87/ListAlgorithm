import { Component, OnInit, EventEmitter, Inject, ElementRef, ViewChild } from '@angular/core';
import { NewECR, ContactInfo, GetCompanyNameResult, ProductAnswerOption, CreateECRRequest, PortalContact, CreateECRResult, AffiliateInfo, ReturnAffiliates, ReturnCountries, CountryInfo, GeneralCallResult } from '../models/misc-models';
import { HttpClient, HttpHeaders, HttpParameterCodec } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';

@Component({
  selector: 'app-newecr',
  templateUrl: './newecr.component.html',
  styleUrls: ['./newecr.component.scss']
})

export class NewecrComponent implements OnInit {
  @ViewChild('clientControl') clientControl: ElementRef;
  @ViewChild('companyNameControl') companyNameControl: ElementRef;
  @ViewChild('productControl') productControl: ElementRef;
  @ViewChild('countriesControl') countriesControl: ElementRef;
  @ViewChild('numberEmpControl') numberEmpControl: ElementRef;
  @ViewChild('pcFirstName') pcFirstName: ElementRef;
  @ViewChild('pcLastName') pcLastName: ElementRef;
  @ViewChild('pcEmail') pcEmail: ElementRef;


  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  private companyNameLookedUp = false;
  public createECRRequestData = new CreateECRRequest;
  public contacts: PortalContact[] = [];
  public selectedItem: ProductAnswerOption;
  public dropdownAnswerOptions: ProductAnswerOption[] = [];
  public createECRDisabled = true;
  public createECRResultMessage = "";
  public waitingForCreateECR = false;
  public waitingForCompanyNameCallToFinish = false;
  public forceDisableCreateButton = false;
  public countries: CountryInfo[] = [];
  public affiliates: AffiliateInfo[] = [];
  public myAffiliate: AffiliateInfo;
  public myCountry: CountryInfo;
  public clientRangeOK = true;
  public message = "";
  public preValidationOnEcrCreationMessage = "";
  public defaultCountryCode = "";
  public EmpNan = false;
  public invalidEmail = false;
  public primaryContactEmailAddressReady = false;
  public waitingforEmailVerification = false;
  public verifiedEmail = "";
  public companyNameEntered = true;
  public invalidEmailMessage = "";
  public contactFirstName = "";
  public contactLastName = "";
  public contactEmail = "";


  constructor(private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
    private router: Router, public bs: BackendService) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;

    //Assess(Standard), Analyze(actually launches AnalyzePlus / Tailored), Accelerate(Unlimited)(user selects this)   

  }

  ngOnInit() {

    AppInsights.trackPageView("NewECR component:ngOnInit");

    if (!this.authService.isClaimValid("idp", 'GptwEmployeeLogin')) {
      this._router.navigate(["/error/noaccess"]);
    }

    if (!this.authService.isClaimValid("GptwAd_GptwAppRole_PortalGeneral", "GptwAppRole_PortalGeneral")) {
      this._router.navigate(["/error/noaccess"]);
    }

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
    }
    );

  }

  onButtonClick() {

    if (this.waitingForCreateECR)
      return;

    this.validateEmail();

    this.createECRDisabled = true;
    this.createECRResultMessage = "";

    this.bs.saveNewECRResult("");
    if (this.waitingforEmailVerification)
      return;
    if (this.invalidEmail)
      return;

    // add portal contact to the request
    this.createECRRequestData.email = this.contactEmail;
    this.createECRRequestData.firstName = this.contactFirstName;
    this.createECRRequestData.lastName = this.contactLastName;
    this.createECRRequestData.achievementNotification = true;

    this.forceDisableCreateButton = true;
    this.waitingForCreateECR = true;

    this._httpClient.post<CreateECRResult>(this._baseUrl + 'api/Portal/CreateECR', this.createECRRequestData, { headers: this.authService.getRequestHeaders() }).subscribe(result => {

      this.createECRDisabled = false;
      this.waitingForCreateECR = false;
      this.preValidationOnEcrCreationMessage = "";

      this.forceDisableCreateButton = false;
      this.verifiedEmail = "";

      if (result) {
        if (result.isError) {
          var err = "Error:" + result.errorStr;
          console.error(err);
          this.createECRResultMessage = err;
        }
        else {
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
        this.createECRResultMessage = err;
      }
    }, error => {
      this.createECRDisabled = false;
      this.waitingForCreateECR = false;
      this.forceDisableCreateButton = false;
      var err = "CreateECR failed with an unhandled error.";
      console.error(err);
      this.createECRResultMessage = err;
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


    if (this.createECRRequestData.clientId !== undefined)
      clientIdReady = true;


    if (this.createECRRequestData.clientName !== undefined && this.createECRRequestData.clientName.length > 0) {
      clientNameReady = true;
    }

    if (this.selectedItem !== undefined) {
      this.createECRRequestData.trustIndexSurveyType = this.selectedItem.value;
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
    if (this.invalidEmail)
      return true;

    return (clientIdReady === false || clientNameReady === false || totalEmployeesReady === false || productTypeReady === false || countryReady === false ||
      primaryContactFirstNameReady === false || primaryContactLastNameReady === false || this.primaryContactEmailAddressReady === false);
  }

  onKeyupEvent() {
  }

  // This allows us to continue entering clientId's for subsequent ECR's. By clearing this flag the company name lookup will run again.
  //onClientIdKeyupEvent()
  //    this.companyNameLookedUp = false;
  //    this.createECRRequestData.clientName = "";
  //}

  affiliateSelectionChanged(selectedItem: AffiliateInfo) {
    let url = this._baseUrl + 'api/Affiliates/GetCountriesForAffiliate?myAffiliateId=' + selectedItem.affiliateId;

    this._httpClient.get<ReturnCountries>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.countries = result.countries;
      }
    }
    );
    //let dummyaffiliate: AffiliateInfo;
    //dummyaffiliate.affiliateId = "US";
    //dummyaffiliate.affiliateName = "dummy";
    //this.affiliates.push(dummyaffiliate);

    this.createECRRequestData.affiliateId = selectedItem.affiliateId;
    this.defaultCountryCode = selectedItem.defaultCountryCode;
    this.dropdownAnswerOptions = [];
    let newAnswerOption: ProductAnswerOption = new ProductAnswerOption();
    newAnswerOption.name = "Assess ";
    newAnswerOption.value = "standard";
    this.dropdownAnswerOptions[0] = newAnswerOption;

    newAnswerOption = new ProductAnswerOption();
    newAnswerOption.name = "Analyze ";
    newAnswerOption.value = "ultratailored";
    this.dropdownAnswerOptions[1] = newAnswerOption;

    newAnswerOption = new ProductAnswerOption();
    newAnswerOption.name = "Accelerate ";
    newAnswerOption.value = "unlimited";
    this.dropdownAnswerOptions[2] = newAnswerOption;

    if (this.createECRRequestData.affiliateId === "US1") {
      newAnswerOption = new ProductAnswerOption();
      newAnswerOption.name = "None (No TI will be created)";
      newAnswerOption.value = "none";
      this.dropdownAnswerOptions[3] = newAnswerOption;
    }

    this.clientControl.nativeElement.value = "";
    this.companyNameControl.nativeElement.value = "";
    this.numberEmpControl.nativeElement.value = "";
    this.pcFirstName.nativeElement.value = "";
    this.pcLastName.nativeElement.value = "";
    this.pcEmail.nativeElement.value = "";
    this.productControl = null;
    this.myCountry = null;
    this.companyNameLookedUp = false;
  }

  employeesChanged() {
    let num = this.numberEmpControl.nativeElement.value;
    if (isNaN(num)) {
      this.EmpNan = true;
      this.createECRRequestData.totalEmployees = undefined;
    }
    else {
      this.createECRRequestData.totalEmployees = Number(num);
      if (this.createECRRequestData.totalEmployees >= 10)
        this.EmpNan = false;
      else
        this.EmpNan = true;
    }
  }

  productSelected(selectedProduct: ProductAnswerOption) {
    this.selectedItem = selectedProduct;
    let url = this._baseUrl + 'api/Portal/GetNewECRCountries?clientId=' + this.createECRRequestData.clientId + '&affiliateId=' + this.createECRRequestData.affiliateId + '&selectedProduct=' + this.selectedItem.value;
    this._httpClient.get<ReturnCountries>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.countries = result.countries;
        if (this.selectedItem.value === 'standard' || this.selectedItem.value === 'ultratailored') {
          this.myCountry = this.countries.find(x => x.countryCode === this.defaultCountryCode);
          this.createECRRequestData.countryCode = this.defaultCountryCode;
        }
      }
    }
    );
  }

  countrySelected(selectedCountry: CountryInfo) {
    this.myCountry = selectedCountry;
    this.createECRRequestData.countryCode = this.myCountry.countryCode;

  }

  verifyClientId() {
    this.clientRangeOK = false;
    this.message = "Client Number Ranges for this Affiliate have not been configured";
    if (!this.isNumeric(this.createECRRequestData.clientId))
      this.message = "ClientId needs to be numeric";
    else {
      if ((this.createECRRequestData.clientId >= this.myAffiliate.startClientId) && (this.createECRRequestData.clientId <= this.myAffiliate.endClientId))
        this.clientRangeOK = true;
      else
        this.message = "ClientId is outside expected range of " + this.myAffiliate.startClientId + " and " + this.myAffiliate.endClientId + " for " + this.myAffiliate.affiliateName;
    }
    if (this.clientRangeOK === false) {
      //this.clientControl.nativeElement.focus();
    }
    else {
      this.preValidateNewECRCreation();
    }
  }

  isNumeric(val: any): val is number | string {
    return (val - parseFloat(val) + 1) >= 0;
  }

  verifyClientName() {
    this.companyNameEntered = true;
    if (this.createECRRequestData.clientName.length == 0)
      this.companyNameEntered = false;
  }

  async validateEmail() {
    if (this.verifiedEmail === this.contactEmail)
      return;
    this.waitingforEmailVerification = true;
    var url = this._baseUrl + 'api/Portal/IsValidEmail?email=' + encodeURIComponent(this.contactEmail);
    await this._httpClient.get<boolean>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result === true || result === false) {
        this.invalidEmail = !result;
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

  public preValidateNewECRCreation() {
    this.preValidationOnEcrCreationMessage = "";
    let url = this._baseUrl + 'api/Portal/PreValidateNewECRCreation?clientId=' + this.createECRRequestData.clientId + '&affiliateId=' + this.createECRRequestData.affiliateId;
    this._httpClient.get<GeneralCallResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.success) {
          this.preValidationOnEcrCreationMessage = result.errorMessage;
        }
        else {
          this.getCompanyName();
        }
      }
    }, error => {
      console.error("newecr.component:PreValidateNewECRCreation unhandled error:");
    }
    )
  }

  getCompanyName() {
    this.companyNameLookedUp = false;
    // if we haven't yet looked up the companyName and we have a clientId (!= 0) then look it up
    if (this.createECRRequestData.affiliateId === 'US1') {
      if (!this.companyNameLookedUp && this.createECRRequestData.clientId !== undefined) {
        this.companyNameLookedUp = true;
        this.waitingForCompanyNameCallToFinish = true;
        //sessionStorage.createECRClientId = this.createECRRequestData.clientId;
        let url = this._baseUrl + 'api/Portal/GetCompanyName?clientId=' + this.createECRRequestData.clientId + '&affiliateId=' + this.createECRRequestData.affiliateId;
        this._httpClient.get<GetCompanyNameResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
          this.waitingForCompanyNameCallToFinish = false;
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
          this.waitingForCompanyNameCallToFinish = false;
          this.createECRRequestData.clientName = "Not Found!"
          console.error("newecr.component:GetCompanyName unhandled error:");
        }
        );
      }
    }

  }

}


