import { Component, Inject, OnInit, ElementRef, ViewChild, HostListener } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { BackendService } from '../services/backend.service';
import { HttpClient, HttpParameterCodec } from '@angular/common/http';
import { FilterService, PageChangeEvent } from '@progress/kendo-angular-grid';
import { SetStatusResult, DefaultClientEmailGridSettings, ClientEmail, GetClientEmailDataResult, AffiliateInfo, ReturnAffiliates, GridSettings, GetDashboardDataResult, FavoriteClickedResult } from '../models/misc-models';
import { FilterDescriptor, CompositeFilterDescriptor, process, State } from '@progress/kendo-data-query';

@Component({
  selector: 'app-listrequest',
  templateUrl: './listrequest.component.html',
  styleUrls: ['./listrequest.component.scss']
})
export class ListRequestComponent implements OnInit {
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

  }
}
