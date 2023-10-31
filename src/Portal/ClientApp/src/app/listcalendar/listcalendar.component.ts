import { Component, OnInit, NgModule, Inject } from '@angular/core';
import { GridModule, GridDataResult } from '@progress/kendo-angular-grid';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { ListCalendar, ListCalendarResult } from '../models/misc-models';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { DatePipe } from '@angular/common';
import { SortDescriptor, orderBy } from '@progress/kendo-data-query';
import { getToday } from '@progress/kendo-angular-dateinputs/dist/es2015/util';

@Component({
    selector: 'app-listcalendar',
    templateUrl: './listcalendar.component.html',
    styleUrls: ['./listcalendar.component.scss']
})
export class ListcalendarComponent implements OnInit {
    public listcalendar: ListCalendar[] = [];
    private clientId: number;
    private _baseUrl: string;
    private _httpClient: HttpClient;
    private _router: Router;
    public dataIsLoading: boolean = false;
    public sort: SortDescriptor[] = [{
        field: 'Name',
        dir: 'asc'
    }];

    constructor(private route: ActivatedRoute, private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
        private router: Router, public bs: BackendService, public datepipe: DatePipe) {
        this._baseUrl = baseUrl;
        this._httpClient = http;
        this._router = router;

        route.params.subscribe(params => {
            this.clientId = params['clientId'];
        })
    }

    ngOnInit() {

        this.bs.setHeaderDependencies("clientId", this.clientId);

        let url: string = this._baseUrl + 'api/Portal/GetListCalendar';
        this.dataIsLoading = true;

        this._httpClient.get<ListCalendarResult>(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
            this.dataIsLoading = false;
            if (result) {
                if (!result.isError) {
                    for (let calitem of result.listCalendar) {
                        let localCal: ListCalendar = new ListCalendar;

                        localCal.name = calitem.name;
                        if (new Date(calitem.certified_by) > getToday()) {
                            localCal.certified_by = this.datepipe.transform(calitem.certified_by, 'mediumDate');
                            localCal.italicize =false;
                        }
                        else { 
                            localCal.certified_by = "Deadline Passed";
                            localCal.italicize = true;
                    }
                        localCal.publish_up = this.datepipe.transform(calitem.publish_up, 'mediumDate');
                        localCal.url = "https://www.greatplacetowork.com" + calitem.url;

                        this.listcalendar.push(localCal);

                    }
                }

                else {
                    this.router.navigate(["/error/general"]);
                }
            } else {
                this.router.navigate(["/error/general"]);
            }
        }, error => {
            this.router.navigate(["/error/general"]);
        }
        );
    }
    //public onClick(e) {
      //  window.location.href = e.dataItem.url;
    //}
}




