import { Component, Inject, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { Claim, GetHelpDeskLoginURLResult } from '../models/misc-models';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';

@Component({
    selector: 'app-helpdesksso',
    templateUrl: './helpdesksso.component.html',
    styleUrls: ['./helpdesksso.component.scss']
})

export class HelpdeskssoComponent implements OnInit {
    private baseUrl: string;
    private httpClient: HttpClient;
    private router: Router;
    private as: AuthService;
    private clientId: number;
    private returnTo: string = "";
    private url: string = "";
    public showLink: boolean = false;

    constructor(public bs: BackendService, private route: ActivatedRoute, private authService: AuthService,
        http: HttpClient, @Inject('BASE_URL') baseUrl: string, private routerRef: Router) {
        this.baseUrl = baseUrl;
        this.httpClient = http;
        this.router = routerRef;
        this.as = authService;
    }

    ngOnInit() {
        AppInsights.trackPageView("Engagement component:ngOnInit");
        let returnTo: string = this.as.getAndThenClearZendeskReturnTo();

        this.returnTo = returnTo;

        if (returnTo.length > 0)
            this.getHelpDeskJWT();
        else {
            this.returnTo = this.bs.getZendeskAPIUrl() + "hc/en-us/articles/360044944113-What-are-these-sections-and-articles-doing-here-"
            this.showLink = true;
        }
    }

    getHelpDeskJWT() {
        this.httpClient.get<GetHelpDeskLoginURLResult>(this.baseUrl + 'api/Zendesk/GetHelpDeskLoginURL?returnTo=' + this.returnTo, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
            if (result) {
                if (result.errorOccurred) {
                    console.log("error:result.errorMessage" + result.errorMessage);
                    this.router.navigate(["/error/general"]);
                }
                else {
                    this.url = result.url;
                    this.bs.navigateExt(this.url);
                }
            }
            else {
                console.log("result undefined")
                this.router.navigate(["/error/general"]);
            }
        }, error => {
            console.error(error);
            this.router.navigate(["/error/general"]);
        }
        );
    }

}

/*


Need to work with Derek and Stephanie to get current state of Zendesk trial account and URLs etc.
Make sure everything is parameterized in config values so we can update URLs and secrets when we move to the full implementation in a week or so.

Zendesk article is detailed here:
https://support.zendesk.com/hc/en-us/articles/203663816


https://dev-my.greatplacetowork.com/helpdesksso?brand_id=360004225454&locale_id=1&return_to=https%3A%2F%2Fdev-greatplacetowork.zendesk.com%2Fhc%2Fen-us%2Farticles%2F360044944113-What-are-these-sections-and-articles-doing-here-&timestamp=1584146286

Most complex flow:
(1) User (client or employee) accesses https://gptw.zendesk.com/tickets/123 in their browser
(2) Zendesk redirects user to https://dev-my.greatplacetowork.com/helpdesksso?return_to=https://gptw.zendesk.com/tickets/123
(3) This is identical to a user trying to access any other page that exists in the portal (possible issue that we are using ? URL parameters)
(4) Assuming user is not signed in their portal URL will be saved in local storage and they will be directed to IdentityServer to log on (client or employee)
(5) User will complete their login at Identity server and be redirected to the auth callback URL in the portal
(6) The auth callback in the portal will sign the user in and send them to /helpdesksso?return_to=https://gptw.zendesk.com/tickets/123 as a signed in user
(7) Controller in /helpdesksso needs to:
(a) Call a WebAPI (probably doesn't need any parameters) which returns the packaged JWT.
(b) Retrieves the "return to" URL from the URL parameter
(c) Concatenates everything together and navigates the user to:
https://gptw.zendesk.com/access/jwt?jwt=JWTDATA&return_to=RETURNTOURL

Need to also plan for and handle the case where there is no return_url in the request.

Initially the WebApi should return the simplest token required with only the absolute minimum fields:
iat, jti, email, name

*/
