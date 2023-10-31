import { Component, OnInit, Input, Inject } from '@angular/core';
import { BackendService } from '../../services/backend.service';
import { EngagementInfo } from '../../models/misc-models';

@Component({
  selector: 'app-hero-message',
  templateUrl: './hero-message.component.html',
  styleUrls: ['./hero-message.component.scss']
})
export class HeroMessageComponent implements OnInit {

  @Input() eInfo: EngagementInfo;

  public heroMessageSelector: string;
  public emprisingSetSurveyDatesLink: string;

  constructor(private bs: BackendService) {
  }

  ngOnInit() {
    this.setHeroMessagingSelector();
  }

  setHeroMessagingSelector() {

    // a. Provide message for customers that choose to opt out of the TI
    // Data/Trigger: Tier = No TI
    if (this.eInfo.tier.toLowerCase() == "no ti") {
      this.heroMessageSelector = "complete-your-cb-no-ti";
      return;
    }

    // b. Set Survey Date
    // Data/Trigger: Journey Status = (CREATED or LOGGED IN) AND Tier IS NOT No TI
    if ((this.eInfo.journeyStatus.toLowerCase() == "created" || this.eInfo.journeyStatus.toLowerCase() == "logged in") && this.eInfo.tier.toLowerCase() != "no ti") {
      this.heroMessageSelector = "lets-get-started";
      return;
    }

    // c. Design Survey
    // Data/Trigger: Journey Status = DATES SELECTED AND Tier IS NOT No TI
    if (this.eInfo.journeyStatus.toLowerCase() == "dates selected" && this.eInfo.tier.toLowerCase() != "no ti") {
      this.heroMessageSelector = "set-up-survey";
      return;
    }

    // d. Design Survey
    // Data/Trigger: Journey Status = LAUNCH APPROVED AND Tier IS NOT No TI
    if (this.eInfo.journeyStatus.toLowerCase() == "launch approved" && this.eInfo.tier.toLowerCase() != "no ti") {
      this.heroMessageSelector = "start-your-cb";
      return;
    }

    // e. Monitor Survey Participation (e.)
    // Data/Trigger: Journey Status = SURVEY LIVE AND Tier IS NOT No TI
    if (this.eInfo.journeyStatus.toLowerCase() == "survey live" && this.eInfo.tier.toLowerCase() != "no ti") {
      this.heroMessageSelector = "survey-is-live";
      return;
    }

    // f. Complete Culture Brief
    // Data/Trigger: Journey Status = SURVEY CLOSED AND Tier IS NOT No TI AND CB is NOT Completed.
    if (this.eInfo.journeyStatus.toLowerCase() == "survey closed" && this.eInfo.tier.toLowerCase() != "no ti" && this.eInfo.cultureBriefStatus.toLowerCase() != 'completed') {
      this.heroMessageSelector = "complete-your-certification";
      return;
    }

    // g. Waiting for certification to process
    // Data/Trigger: (Journey Status = SURVEY CLOSED OR COMPLETE OR ACTIVATED) AND (Tier IS NOT No TI) AND (CB is Completed) AND (Certification Status = PENDING OR blank).
    if ((this.eInfo.journeyStatus.toLowerCase() == "survey closed" || this.eInfo.journeyStatus.toLowerCase() == "complete" || this.eInfo.journeyStatus.toLowerCase() == "activated") && this.eInfo.tier.toLowerCase() != "no ti" && this.eInfo.cultureBriefStatus.toLowerCase() == 'completed') {
      if (this.eInfo.certificationStatus.toLowerCase() == "pending" || this.eInfo.certificationStatus.toLowerCase() == "") {
        this.heroMessageSelector = "completed-submission";
        return;
      }
    }

    // h. You have opted out
    // Data/Trigger: Journey Status = INCOMPLETE
    if (this.eInfo.journeyStatus.toLowerCase() == "incomplete") {
      this.heroMessageSelector = "opted-out";
      return;
    }

    // Show default message if no other conditions apply
    this.heroMessageSelector = "default";

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
      case "setStartDates": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/1500002892921-Set-Survey-Dates");
        break;
      }
      case "emprisingDeepLinkSetSurveyDates": {
        this.bs.navigateExtNewWindow(this.eInfo.trustIndexSSOLink + '/assessment-design/survey-design?section=schedule');
        break;
      }
      case "emprisingDeepLinkSurveySetup": {
        this.bs.navigateExtNewWindow(this.eInfo.trustIndexSSOLink + '/assessment-design/survey-design');
        break;
      }
      case "whoToInclude": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360055760233-Who-to-Include-in-Your-Survey");
        break;
      }
      case "howToCommunicate": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360043616853-Tell-Your-Organization-the-Survey-is-Coming");
        break;
      }
      case "setUpYourTechnology": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360043617333-IT-Checklist-and-Test-Emails-");
        break;
      }
      case "participationGuide": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/1500003300142-Important-participation-guide-for-companies-with-350-or-fewer-employees");
        break;
      }
      case "yourEmployeeSurvey": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/categories/1500000355141-YOUR-EMPLOYEE-SURVEY");
        break;
      }
      case "knowledgeBase": {
        this.bs.navigateExtNewWindow("https://www.greatplacetowork.com/kb")
        break;
      }
      case "monitorSurveyParticipation": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360055917413-Participation-Monitoring");
        break;
      }
      case "manageInvitations": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360049555233-Troubleshooting-Survey-Invitation-Issues");
        break;
      }
      case "sendReminders": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360052880493-Maximizing-Survey-Participation");
        break;
      }
      case "surveyExtensions": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/articles/360052880493-Maximizing-Survey-Participation");
        break;
      }
      case "manageLiveSurvey": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/sections/360011688794-Manage-Live-Survey");
        break;
      }
      case "emprisingDeepLinkMonitorSurvey": {
        this.bs.navigateExtNewWindow(this.eInfo.trustIndexSSOLink + '/assessment-monitoring/participation-monitor');
        break;
      }
      case "emprisingDeepLinkCommunications": {
        this.bs.navigateExtNewWindow(this.eInfo.trustIndexSSOLink + '/assessment-monitoring/emails-and-campaigns');
        break;
      }
      case "readCBSection": {
        this.bs.navigateExtNewWindow("https://help.greatplacetowork.com/hc/en-us/sections/360011529893-Culture-Brief-");
        break;
      }
      case "cbDirectLink": {
        this.bs.navigateExtNewWindow(this.eInfo.cultureBriefSSOLink);
      }
    }
  }
}
