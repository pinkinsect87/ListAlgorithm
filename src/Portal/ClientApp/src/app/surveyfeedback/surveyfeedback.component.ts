import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { CreateHelpDeskTicketResult } from '../models/misc-models';
import { WindowRef } from '../services/auth.service';


@Component({
    selector: 'app-surveyfeedback',
    templateUrl: './surveyfeedback.component.html',
    styleUrls: ['./surveyfeedback.component.scss']
})
export class SurveyfeedbackComponent implements OnInit {
    private _baseUrl: string;
    private _httpClient: HttpClient;
    private _router: Router;
    private _winRef: WindowRef;
    public dataIsLoading: boolean = false;
    public name: string = ""
    public email: string = "";
    public company: string = "";
    public phone: string = "";
    public comment: string = "";
    public submitted: boolean = false;
    public submissionError: boolean = false;

    constructor(private route: ActivatedRoute, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
        private router: Router, winRef: WindowRef) {
        this._baseUrl = baseUrl;
        this._httpClient = http;
        this._router = router;
        this._winRef = winRef;
    }

    ngOnInit() {
    }

    onButtonClick() {
        this.SubmitFeedback();
    }

    closeConfirmationDialog() {
        //this doesn't work
        //this._winRef.nativeWindow.close();

        this._winRef.nativeWindow.location.href = 'https://www.greatplacetowork.com';
    }

    public SubmitFeedback() {
        this._httpClient.get<boolean>(this._baseUrl + 'api/Zendesk/CreateHelpdeskTicket?email=' + this.email + '&comment=' + this.comment + '&companyname=' + this.company + '&name=' + this.name + '&phone=' + this.phone).subscribe(result => {
            if (true) {
                this.submitted = true;
            }
            else {
            }
        }, error => {
            this.submissionError = true;
            console.error(error);
        }
        );
    }

}

