import { Component, Inject, OnInit } from '@angular/core';
import { AppInsights } from 'applicationinsights-js';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { BackendService } from '../services/backend.service';
import { HttpClient } from '@angular/common/http';
import { ClientConfiguration } from '../models/misc-models';

@Component({
  selector: 'app-error',
  templateUrl: './error.component.html',
  styleUrls: ['./error.component.scss']
})

export class ErrorComponent implements OnInit {
    private baseUrl: string;
    private httpClient: HttpClient;
    public errorType = "general";
  private supportedErrorTypes: Array<string> = ['noengagement', 'noportalclaim', 'sessionexpired', 'noaccess', 'general', 'notoolkitaccess', 'downformaintenance', 'tokenexpiredatlogin', 'invalidfile'];

  constructor(public bs: BackendService, private route: ActivatedRoute, private authService: AuthService, 
        private router: Router, http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
        this.httpClient = http;
        route.params.subscribe(params => {
          let param: string = params['errorType'];
          if (param !== undefined && param.length > 0) {
            param = param.toLowerCase();
            if (this.supportedErrorTypes.includes(param)) {
              this.errorType = param;
            }
          }
        })
    }

    ngOnInit() {
        let clientId = this.authService.getNonRoleClaim("gptw_client_id");
        let email = this.authService.getNonRoleClaim("email");
        AppInsights.trackPageView("Error component:User:" + email + ",cid:" + clientId );
    }

    navigateToTheRoot() {
      this.router.navigate(["/"]);
    }

navigateToEmprising(): void {
  this.bs.navigateExt(this.bs.config.EmprisingUrl);

  }

}
