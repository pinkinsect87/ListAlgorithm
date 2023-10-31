import { Component, OnInit } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-newecrdone',
  templateUrl: './newecrdone.component.html',
  styleUrls: ['./newecrdone.component.scss']
})
export class NewECRDoneComponent implements OnInit {
  public newECRResult: string = "";
  private router: Router;

  constructor(public bs: BackendService, private routerRef: Router) {
    this.router = routerRef
  }

  ngOnInit() {
    this.newECRResult = this.bs.getNewECRResult();

  }

  onClickRoute(routeStr: string) {
    switch (routeStr) {
      case "return": {
        //this.router.navigate(["/admin/newecr"]);
        this.router.navigate(["/admin/newecr2"]);
        break;
      };
      case "dashboard": {
        this.router.navigate(["/employee/dashboard/"]);
        break;
      }
      case "portal": {
        this.router.navigate(["/certify/engagement/" + this.bs.newECREngagementId ]);
        break;
      }
    }
  }

}
