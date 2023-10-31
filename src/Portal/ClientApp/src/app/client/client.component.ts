import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { BackendService } from '../services/backend.service';
import { GetCertAPPInfoResult, CertificationApplication } from '../models/misc-models';

@Component({
  selector: 'app-client',
  templateUrl: './client.component.html',
  styleUrls: ['./client.component.scss']
})

export class ClientComponent implements OnInit {
  private baseUrl: string;
  private httpClient: HttpClient;
  private router: Router;
  private clientId: number;
  public openApplications: CertificationApplication[];
  public submittedApplications: CertificationApplication[];
  public isShowNote: boolean = false;

  constructor(public bs: BackendService, private route: ActivatedRoute, private authService: AuthService,
    http: HttpClient, @Inject('BASE_URL') baseUrl: string, private routerRef: Router) {
    this.baseUrl = baseUrl;
    this.httpClient = http;
    this.router = routerRef;

    route.params.subscribe(params => {
      this.clientId = params['clientId'];
    })
  }

  ngOnInit() {
    this.bs.setHeaderDependencies("clientId", this.clientId);

    this.httpClient.get<GetCertAPPInfoResult>(this.baseUrl + 'api/Portal/GetCertAPPInfo?clientId=' + this.clientId, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.errorOccurred) {
          this.router.navigate(["/error/general"]);
        }

        this.openApplications = result.openApplications;
        this.submittedApplications = result.submittedApplications;

        var openAppsSurveyingCountry = this.openApplications.find(p => p.countries.includes("*"));
        var submittedAppsSurveyingCountry = this.submittedApplications.find(p => p.countries.includes("*"));
        if ((openAppsSurveyingCountry != undefined && openAppsSurveyingCountry != null && !openAppsSurveyingCountry.isCA) ||
          (submittedAppsSurveyingCountry != undefined && submittedAppsSurveyingCountry != null && !submittedAppsSurveyingCountry.isCA))
          this.isShowNote = true;
        else
          this.isShowNote = false;
      }
      else {
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

  blockClicked(app) {
    if (app.isCA) {
      this.bs.navigateExt(app.cultureAuditLink);
    }
    else if (app.isCB) {
      this.bs.navigateExt("/certify/engagement/" + app.engagementId);
    }
  }
}
