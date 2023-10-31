import { Component, OnInit, Input, Inject } from '@angular/core';
import { BackendService } from '../../services/backend.service';

@Component({
  selector: 'app-quick-links',
  templateUrl: './quick-links.component.html',
  styleUrls: ['./quick-links.component.scss']
})
export class QuickLinksComponent implements OnInit {

  @Input() clientId: Number;
  @Input() isUS: boolean;

  constructor(private bs: BackendService) {
  }

  ngOnInit() {
  }

  onClickRoute(routeStr: string) {
    switch (routeStr) {
      case "onboardingVideo": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb/onboardingvideo");
        break;
      }
      case "tourofthePortal": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb/portaltour");
        break;
      }
      case "HeroMessageGuide": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360046650434-hero-message-with-Certification-Guide");
        break;
      }
      case "knowledgeBase": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb")
        break;
      }
      case "listDeadlines": {
        this.bs.navigateExtNewWindow("/listcalendar/" + this.clientId);
        break;
      }
    }
  }
}
