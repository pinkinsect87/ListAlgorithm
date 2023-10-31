import { AuthService } from '../../services/auth.service';
import { Component, Inject, OnInit } from '@angular/core';
import { DataExtractRequestComponent } from '../../dataextractrequest/dataextractrequest.component';
import { GetDataExtractCompanyInfoByClientIdResult, DataExtractCompanyInfo } from '../../models/misc-models';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-dataextractrequest-company',
  templateUrl: './dataextractrequest-company.component.html',
  styleUrls: ['./dataextractrequest-company.component.scss']
})
export class DataExtractRequestCompanyComponent implements OnInit {
  private _baseUrl: string;
  private _httpClient: HttpClient;

  public unique_key: number;
  public parentRef: DataExtractRequestComponent;
  public isShowRemove: boolean = true;
  public clientId: number;
  public companyName: string;
  public dataExtractCompanyInfo: DataExtractCompanyInfo[] = [];
  public engagement: DataExtractCompanyInfo = new DataExtractCompanyInfo;
  public isValid: boolean = false;
  
  constructor(private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
  }

  ngOnInit() { }

  remove() {
    this.parentRef.removeCompanyComponent(this.unique_key);
  }

  clientIdChanged() {
    this.engagement = undefined;
    this.getDataExtractCompanyInfoByClientId(this.clientId);
    this.companyName = "";
    this.isValid = false;
  }

  engagementIdSelectionChanged(selection: DataExtractCompanyInfo) {
    this.companyName = selection.clientName;
    this.isValid = true;
  }

  getDataExtractCompanyInfoByClientId(clientId) {
    let url = this._baseUrl + 'api/Portal/GetDataExtractCompanyInfoByClientId?clientId=' + clientId;

    this._httpClient.get<GetDataExtractCompanyInfoByClientIdResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        this.dataExtractCompanyInfo = result.dataExtractCompanyInfos;
      }
    });
  }
}
