import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { GetClientRecognitionInfoResult, ClientRecognitionInfoDetail } from '../models/misc-models';
import { DatePipe } from '@angular/common';


@Component({
    selector: 'app-recognition',
    templateUrl: './recognition.component.html',
    styleUrls: ['./recognition.component.scss']
})
export class RecognitionComponent implements OnInit {
    private _baseUrl: string;
    private _httpClient: HttpClient;
    private _router: Router;
    public clientId: number;
    public clientRecognitionInfoDetails: ClientRecognitionInfoDetail[];
    public baseListUrl: string = "https://www.greatplacetowork.com";
    public isLoading: boolean = false;

    constructor(private route: ActivatedRoute, private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
        private router: Router, public bs: BackendService, public datePipe: DatePipe) {
        this._baseUrl = baseUrl;
        this._httpClient = http;
        this._router = router;

        route.params.subscribe(params => {
            this.clientId = params['clientId'];
        })
    }

    ngOnInit() {
        this.bs.setHeaderDependencies("clientId", this.clientId);

        this.isLoading = true;

        let url: string = this._baseUrl + 'api/Recognition/GetClientListRecognition?clientId=' + this.clientId;

        this._httpClient.get<GetClientRecognitionInfoResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
            if (result) {
                if (!result.isError) {

                    // create an array of list recognitions that can be presented to the client
                    // this specifically means it is now after the dashboard date

                    let now: Date = new Date();
                    let allRecognitions = result.clientRecognitionInfoDetails;
                    let presentableRecognitions: ClientRecognitionInfoDetail[] = [];

                    let i: any;
                    for (i in allRecognitions) {
                        let recognition = allRecognitions[i];
                        let dashboardDate: Date = new Date(recognition.dashboard_date);
                        if (dashboardDate.getTime() <= now.getTime()) {
                            presentableRecognitions.push(recognition);
                        }
                    }
                    this.isLoading = false;
                    this.clientRecognitionInfoDetails = presentableRecognitions;


                    this.doSomething();
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

    doSomething() {
        //do stuff here once the data is returned from the API call
    }

    getListUrl(crid: ClientRecognitionInfoDetail) {
        var url: string;
        url = this.baseListUrl.concat(crid.list_url).concat("/").concat(String(crid.list_year));
        return url;
    }

    getToolkitUrl(crid: ClientRecognitionInfoDetail) {
        var url: string;
        var re = /best-workplaces/gi;
        url = this.baseListUrl.concat(crid.list_url.replace(re, "toolkit")).concat("/").concat(String(crid.list_year)).concat("?client_id=").concat(String(this.clientId));
        return url;
    }

    onViewCertificationToolkitClick(yearlyListId: number) {
        var url: string = "/recognition/certificationtoolkit/".concat(String(yearlyListId));
        this.router.navigate([url]);
        return false;
    }

    isEmbargoed(recognition: ClientRecognitionInfoDetail) {

        let isEmbargoed: boolean = true;
        let now: Date = new Date();

        // check if after the embargo date
        let publicationDate: Date = new Date(recognition.publication_date);
        if (publicationDate.getTime() <= now.getTime()) {
            isEmbargoed = false;
        }
        return isEmbargoed;
    }

    getFormattedPublicationDate(recognition: ClientRecognitionInfoDetail): string {

        // Format for EST
        let formattedDate: string = this.getPublicationDateFormattedForTimezone(recognition, "EST", "GMT-5");
        return formattedDate;
    }

    getPublicationDateFormattedForTimezone(recognition: ClientRecognitionInfoDetail, timezone: string, timezoneOffset: string): string {

        let formattedDate: string = "";
        let publication_Date: Date = new Date(recognition.publication_date);

        // get the date's UTC offset in hours
        // if already a UTC date the offset will be 0
        let dateParts: string[] = publication_Date.toString().split("GMT");
        let offsetUTCstr: string = dateParts[1].substr(0, 3);
        let offsetUTC: number = Number(offsetUTCstr);
        let dateUTC: Date = new Date(publication_Date.setHours(publication_Date.getHours() + offsetUTC));

        // express the date in the specified timezone
        formattedDate = this.datePipe.transform(dateUTC, "EEEE, MMM d, y, h a z", timezone);
        // replace the formatted date's UTC offset with the actual timezone abbreviation
        formattedDate = formattedDate.replace(timezoneOffset, timezone);
        let dateFormattedParts: string[] = formattedDate.split(", ");
        formattedDate = dateFormattedParts[1] + ", " + dateFormattedParts[2];
        return formattedDate;
    }

}
