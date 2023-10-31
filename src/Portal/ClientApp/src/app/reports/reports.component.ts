import { Component, Inject, OnInit, NgModule, AfterViewInit, OnChanges, SimpleChanges, Input } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AuthService } from '../services/auth.service';
import { HttpClient, HttpHeaders, HttpResponse } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { UserEventName, Claim, FindCurrentEngagementInfoResult, GetEngagementInfoResult } from '../models/misc-models';
import { ReturnTree, ClientReportTreeNode, ClientReportDownload } from '../models/clientreports-models';
import { AppInsights } from 'applicationinsights-js';
import { TreeViewModule } from '@progress/kendo-angular-treeview';
import { of, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { saveAs } from 'file-saver';
import { BackendService } from '../services/backend.service';


@Component({
  selector: 'app-reports',
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss']
})
export class ReportsComponent implements OnInit, OnChanges {
  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  public clientId: string;
  public clientReturnTree: ClientReportTreeNode[] = [];
  public clientReturnTreeJson: any[];
  public expandedKeys: any[] = [];
  public selectedKeys: any[] = [];
  public noReports: boolean = false;
  public dataIsLoading: boolean = false;


  constructor(private route: ActivatedRoute, private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string, private router: Router, public bs: BackendService) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;

    route.params.subscribe(params => {
      this.clientId = params['clientId'];
    })

  }

  ngOnInit() {
    this.bs.setHeaderDependencies("clientId", parseInt(this.clientId, 10));
    this.GetClientReports();
  }

  public GetClientReports() {
    this.dataIsLoading = true;

    this._httpClient.get<ReturnTree>(this._baseUrl + 'api/Reports/GetClientReportTree?clientId=' + this.clientId, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result.isSuccess) {
        var clientReturnTreeJsonString = JSON.stringify(result.clientReportTree);
        this.clientReturnTreeJson = JSON.parse(clientReturnTreeJsonString);
        this.clientReturnTree[0] = result.clientReportTree[0];
        this.dataIsLoading = false;
        if (!result.clientReportTree[0])
          this.noReports = true;
        else
          this.noReports = false;
      }
      else {
        console.log("result undefined")
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
    )
  }

  public expanded(node: ClientReportTreeNode): boolean {
    if (!node)
      return false;
    else
      return node.expanded;
  }

  public fetchChildren(node: any): Observable<any[]> {
    //Return the items collection of the parent node as children.
    if (!this.noReports)
      return of(node.items);
  }

  public isItemSelected = (_: any, index: string) => this.selectedKeys.indexOf(index) > -1;


  //getClientNode(node: string): ClientReportTreeNode {
  //    for (let clientNode of this.clientReturnTree) {
  //        if (clientNode.text == node) {
  //            return clientNode;
  //        }
  //        else {
  //            for (let childnode of clientNode.items) {
  //                this.getClientNode(childnode.text);
  //            }
  //        }
  //    }
  //}

  // I was having problems using the node as input parameter because on one hand the immediate window tells me I have to access the properties using node.dataItem.whatever and
  // on the other hand intellisense and the compiler tells me they are not available.
  // {index} is a bit more kludgy as it builds the index base on your tree selection - eg "0_0_0_3" means you've selected the first 3 branches then the 4th record of the third branch...
  // But at least it works...

  public handleSelection({ index }: any): void {

    let selectedNode: ClientReportTreeNode = this.getSelectedNode(index);
    if (selectedNode.nodeType === 'File') {
      this.dataIsLoading = true;
      let publishingGroupId: any = selectedNode.publishingGroupId;
      let pubGroupReportid: any = selectedNode.pubGroupReportid;
      let url = 'api/Reports/DownloadClientReport?clientId=' + this.clientId + "&fileUri=" + selectedNode.nodeFileUri + "&affiliateId=" + selectedNode.affiliateId;
      this._httpClient.get<Blob>(url, { headers: this.authService.getRequestHeaders(), responseType: 'blob' as 'json' }).subscribe(result => {
        var test = result;

        var blob = new Blob([result], { type: 'application/octet-stream' });
        this.bs.postUserEvent(UserEventName.engagementReportDownload);
        saveAs(blob, selectedNode.fullFileName);
        this.dataIsLoading = false;
      }, error => {
        console.error(error);
        this.router.navigate(["/error/general"]);
      }
      )
    }

  }



  private getSelectedNode(myKeys: string): ClientReportTreeNode {
    let keys = myKeys.split("_");
    let currentNode: ClientReportTreeNode = this.clientReturnTree[0];
    for (var i = 1; i < keys.length; i++) {
      currentNode = currentNode.items[keys[i]];
    }
    return currentNode;

  }

  public handleExpand({ index }: any) {
    let myexpanded = this.expandedKeys;
    let keys = index.split("_");
    let currentNode: ClientReportTreeNode = this.clientReturnTree[0];
    for (var i = 1; i < keys.length; i++) {
      currentNode = currentNode.items[keys[i]];
    }
    currentNode.expanded = true;
  }

  public handleCollapse({ index }: any) {

    let myexpanded = this.expandedKeys;
    let keys = index.split("_");
    let currentNode: ClientReportTreeNode = this.clientReturnTree[0];
    for (var i = 1; i < keys.length; i++) {
      currentNode = currentNode.items[keys[i]];
    }
    currentNode.expanded = false;

  }

  ngOnChanges(changes: SimpleChanges) {


  }

}
