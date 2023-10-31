import { Component, OnInit, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { CountryInfo } from '../../models/misc-models';
import { NewEcrComponent2 } from '../../newecr2/newecr2.component';

@Component({
  selector: 'app-survey-country',
  templateUrl: './survey-country.component.html',
  styleUrls: ['./survey-country.component.scss']
})
export class SurveyCountryComponent implements OnInit {
  public unique_key: number;
  public parentRef: NewEcrComponent2;
  public countries: CountryInfo[] = [];
  public country: CountryInfo;
  public employeeCount: number;
  public isApplyForCertification: boolean = false;
  public isApplyForCertificationDisabled: boolean = false;
  public isShowRemove: boolean = true;
  public isEmployeeCountInvalid = false;
  public employeeCountErrorMessage = "This field is required.";
  public isCountryInvalid = false;
  public countryErrorMessage = "Duplicate country selected.";

  constructor() { }

  ngOnInit() { }

  remove() {
    this.parentRef.removeSurveyCountryComponent(this.unique_key);
  }

  countrySelected(selectedCountry: CountryInfo) {
    this.country = selectedCountry;
    this.parentRef.createECRRequestData.countryCode = this.country.countryCode;
    this.validateDuplicateCountry(selectedCountry.countryCode);
    this.validateEmployeeCount();
  }

  employeeCountChanged() {
    if (this.country !== undefined && this.country !== null) {
      this.validateEmployeeCount();
    }
  }

  isApplyForCertificationChange(e) {
    this.validateEmployeeCount();
  }

  validateDuplicateCountry(countryCode) {
    this.isCountryInvalid = false;
    if (this.parentRef.componentsReferences && this.parentRef.componentsReferences.length > 1) {
      if (this.parentRef.componentsReferences.filter(p => p.instance.unique_key !== this.unique_key && p.instance.country.countryCode == countryCode).length > 0) {
        this.isCountryInvalid = true;
      }
      else {
        var components = this.parentRef.componentsReferences.filter(p => p.instance.isCountryInvalid);
        for (let component of components) {
          component.instance.isCountryInvalid = false;
        }
      }
    }
  }

  validateEmployeeCount() {
    if (this.isApplyForCertification &&
      (this.employeeCount === undefined || this.employeeCount === null || this.employeeCount < 10)) {
      this.isEmployeeCountInvalid = true;
      if (this.employeeCount !== undefined && this.employeeCount < 10) {
        this.employeeCountErrorMessage = "Applying for Certification requires 10 or more employees";
      }
    }
    else if (this.employeeCount === undefined || this.employeeCount === null) {
      this.isEmployeeCountInvalid = true;
      this.employeeCountErrorMessage = "This field is required.";
    }
    else {
      this.isEmployeeCountInvalid = false;
      this.parentRef.createECRRequestData.totalEmployees = this.employeeCount;
    }
  }
}
