import { Component, OnInit, Inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParameterCodec } from '@angular/common/http';
import { AuthService } from '../services/auth.service'
import { ActivatedRoute, Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';
import { BackendService } from '../services/backend.service';
import { UserEventName, PostUserEventResult, Claim, EmailToolkitResult, FindMostRecentCertificationEIDResult, ReturnCountry, CountryInfo } from '../models/misc-models';

@Component({
  selector: 'app-certificationtoolkit',
  templateUrl: './certificationtoolkit.component.html',
  styleUrls: ['./certificationtoolkit.component.scss']
})

export class CertificationToolkitComponent implements OnInit {
  private _baseUrl: string;
  private _httpClient: HttpClient;
  private _router: Router;
  public clientId: string;
  public engagementId: string;
  public certDate: string;
  public yearlyListId: number;
  public socialImages: string;
  public toEmail: string = "";
  public referrer: string = "";
  public referrerEmail: string = "";
  public celebrateLink: string = "https://www.greatplacetowork.com/resources/blog/7-ways-to-celebrate-great-place-to-work-certification";
  public usageLink: string = "https://www.greatplacetowork.com/certification-brand-guide";
  public celebrationKitLink: string = "https://www.greatplacetowork.com/certification/celebration-kit";
  public pressReleaseLink: string = "https://www.greatplacetowork.com/certification/press-release";
  public termsAndConditionsLink: string = "https://www.greatplacetowork.com/certification-best-workplaces-lists-terms";
  public imageTemplate1: string = "Shareable-Image-1-template.svg";
  public imageTemplate2: string = "Shareable-Image-2-template.svg";
  public imageTemplate3: string = "Shareable-Image-3-template.svg";
  public imageTemplate4: string = "Shareable-Image-4-template.svg";
  public outputFilename1: string = "ShareableImage1.png";
  public outputFilename2: string = "ShareableImage2.png";
  public outputFilename3: string = "ShareableImage3.png";
  public outputFilename4: string = "ShareableImage4.png";
  public shareableImageButtonEnabledCssClass: string = "certification-toolkit-button";
  public shareableImageButtonDisabledCssClass: string = "certification-toolkit-button-disabled";

  // Temporarily disable badge and shareable images buttons when clicked
  public badgeBusy: boolean = false;
  public filename1Busy: boolean = false;
  public filename2Busy: boolean = false;
  public filename3Busy: boolean = false;
  public filename4Busy: boolean = false;

  // Disable badge and shareable images buttons if unable to generate
  public badgeError: boolean = false;
  public filename1Error: boolean = false;
  public filename2Error: boolean = false;
  public filename3Error: boolean = false;
  public filename4Error: boolean = false;

  public isEmailSent: boolean = false;
  public isEmailSendError: boolean = false;
  profileImages: string;
  public imageGallerySocialMediaHeight: string;
  public imageGallerySocialMediaWidth: string;
  public imageGalleryProfilesHeight: string;
  public imageGalleryProfilesWidth: string;
  public pageReady: boolean;
  public isUSAffiliate: boolean;
  public isUSCertification: boolean;
  public certCountryCode: string;
  public facebookLink: string;
  public instagramLink: string;
  public linkedInLink: string;
  public twitterLink: string;
  public sendEmailErrorMessage: string;

  constructor(private route: ActivatedRoute, private authService: AuthService, http: HttpClient, @Inject('BASE_URL') baseUrl: string,
    private router: Router, public bs: BackendService) {
    this._baseUrl = baseUrl;
    this._httpClient = http;
    this._router = router;
    this.referrer = authService.user.profile.given_name;
    this.referrerEmail = authService.user.profile.email;
    if (this.referrerEmail == null)
      this.referrerEmail = authService.user.profile.upn;
    route.params.subscribe(params => {
      this.clientId = params['clientId'];
    })

    //this.socialImages = "https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/mrcooper-tweet.jpg,https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/twillo-tweet.jpg,https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/sagent-linkedin.png,https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/innovate.png,https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/awardco-tweet.jpg,https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/MrCooper_carousel.JPG,https://s3.amazonaws.com/media.greatplacetowork.com/images/toolkit/bluecrossnyc-ig.jpeg";
    this.socialImages = "./assets/images/social_media_posts/Instagram_1.png,./assets/images/social_media_posts/Instagram_2.png,./assets/images/social_media_posts/Instagram_3.png,./assets/images/social_media_posts/LinkedIn_1.png,./assets/images/social_media_posts/LinkedIn_2.png,./assets/images/social_media_posts/Twitter_1.png,./assets/images/social_media_posts/Twitter_2.png,./assets/images/social_media_posts/Twitter_3.png";
    this.profileImages = "./assets/images/toolkit/ABC_CompanyProfile.png,./assets/images/toolkit/LCS-fb.png,./assets/images/toolkit/masonic-tw.png";
  }

  ngOnInit() {
    this.bs.setHeaderDependencies("clientId", parseInt(this.clientId, 10));

    const cid = parseInt(this.clientId);
    this._httpClient.get<FindMostRecentCertificationEIDResult>(this._baseUrl + 'api/Portal/FindMostRecentCertificationEID?clientId=' + cid, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.errorOccurred) {
          this._router.navigate(["/error/general"]);
        }
        else {
          if (!result.foundCertificationEngagement)
            this._router.navigate(["/error/notoolkitaccess"]);
          else {
            // allow access if the last certification expired within the last two years
            let recentCutoffDate: Date = new Date();
            recentCutoffDate.setFullYear(recentCutoffDate.getFullYear() - 2);
            let lastCertExpiryDate: Date = new Date(result.certExpiryDate);
            if (lastCertExpiryDate < recentCutoffDate)
              // last certification was more than two years ago
              this._router.navigate(["/error/notoolkitaccess"]);
            this.engagementId = result.engagementId;
            this.certDate = result.certDate;
            this.isUSAffiliate = (result.affiliateId == "US1");
            this.certCountryCode = result.certCountryCode;
            this.isUSCertification = (this.certCountryCode == "US");

            // Set the country-specific social media links
            this.setSocialMediaLinks(this.certCountryCode);

            this.imageGallerySocialMediaHeight = "540px";
            this.imageGallerySocialMediaWidth = "319px";
            this.imageGalleryProfilesHeight = "360px";
            this.imageGalleryProfilesWidth = "100%";
            this.pageReady = true;
            // This was causing an error because the cid/eid were not yet available on a refresh
            // so this work is now being done in the FindMostRecentCertificationEID controller
            //this.bs.postUserEvent(UserEventName.toolkitPageView);
          }
        }
      }
      else {
        this._router.navigate(["/error/general"]);
      }
    }, error => {
      console.error(error);
      this._router.navigate(["/error/general"]);
    });
  }

  async setSocialMediaLinks(certCountryCode: string) {
    let countryInfo: CountryInfo;
    await this._httpClient.get<ReturnCountry>(this._baseUrl + 'api/Affiliates/GetCountryByCountryCode?countryCode=' + certCountryCode, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (!result.isSuccess) {
          this._router.navigate(["/error/general"]);
        }
        else {
          this.facebookLink = result.country.facebookLink;
          this.instagramLink = result.country.instagramLink;
          this.linkedInLink = result.country.linkedInLink;
          this.twitterLink = result.country.twitterLink;
        }
      }
      else {
        this._router.navigate(["/error/general"]);
      }
    }, error => {
      console.error(error);
      this._router.navigate(["/error/general"]);
    });
  }

  async onSendEmailButtonClick() {

    var url = this._baseUrl + 'api/Portal/IsValidEmail?email=' + encodeURIComponent(this.toEmail);
    await this._httpClient.get(url, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result === true || result === false) {
        if (result === false) {
          this.sendEmailErrorMessage = "Invalid email. Use only A-Z 0-9 ~!$%^&*_=+}{'?-.@"
          this.isEmailSendError = true;
          this.isEmailSent = false;
        }
        else {
          if (this.sendBadgeEmail()) {
            this.bs.postUserEvent(UserEventName.toolkitShareToolkit);
            this.isEmailSent = true;
            this.isEmailSendError = false;
          }
          else {
            this.isEmailSendError = true;
            this.isEmailSent = false;
            this.sendEmailErrorMessage = "An error occurred. Please try again or contact support.";
          }
        }
        return result;
      }
    }, error => {
      if (error.status === 401) {
        this.router.navigate(["/error/noaccess"]);
      }
      else {
        this.router.navigate(["/error/general"]);
      }
      return false;
    });
  }

  onClick(linkName: string) {
    let returnValue: boolean = true;
    switch (linkName) {
      case "celebrationToolkit": {
        //this.bs.postUserEvent(UserEventName.toolkitCelebrationKit);
        //this.bs.navigateExtNewWindow(this.celebrationKitLink);
        break;
      }
      case "badgeLinkSVG": {
        if (!this.badgeBusy) {
          this.badgeBusy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadBadgeSVG);
          setTimeout(() => this.badgeBusy = false, 5000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "badgeLinkJPG": {
        if (!this.badgeBusy) {
          this.badgeBusy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadBadgeJPG);
          setTimeout(() => this.badgeBusy = false, 5000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "badgeLinkPNG": {
        if (!this.badgeBusy) {
          this.badgeBusy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadBadgePNG);
          setTimeout(() => this.badgeBusy = false, 5000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "badgeLinkZIP": {
        if (!this.badgeBusy) {
          this.badgeBusy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadBadgeZIP);
          setTimeout(() => this.badgeBusy = false, 10000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "usageLink": {
        this.bs.postUserEvent(UserEventName.toolkitViewUsageGuidlines);
        this.bs.navigateExtNewWindow(this.usageLink);
        break;
      }
      case "pressRelease": {
        this.bs.postUserEvent(UserEventName.toolkitViewPressRelease);
        this.bs.navigateExtNewWindow(this.pressReleaseLink);
        break;
      }
      case "storeLink": {
        this.bs.postUserEvent(UserEventName.toolkitVisitStore);
        this.bs.navigateExtNewWindow("http://www.greatplacetowork.com/store");
        break;
      }
      case "downloadShareableImage1": {
        if (!this.filename1Busy && !this.filename1Error) {
          this.filename1Busy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadShareableImage1);
          setTimeout(() => this.filename1Busy = false, 10000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "downloadShareableImage2": {
        if (!this.filename2Busy && !this.filename2Error) {
          this.filename2Busy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadShareableImage2);
          setTimeout(() => this.filename2Busy = false, 10000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "downloadShareableImage3": {
        if (!this.filename3Busy && !this.filename3Error) {
          this.filename3Busy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadShareableImage3);
          setTimeout(() => this.filename3Busy = false, 10000);
        }
        else {
          returnValue = false;
        }
        break;
      }
      case "downloadShareableImage4": {
        if (!this.filename4Busy && !this.filename4Error) {
          this.filename4Busy = true;
          this.bs.postUserEvent(UserEventName.toolkitDownloadShareableImage4);
          setTimeout(() => this.filename4Busy = false, 10000);
        }
        else {
          returnValue = false;
        }
        break;
      }
    }
    return returnValue;
  }

  imageLoadFailure(imageItem: string) {
    switch (imageItem) {
      case "ShareableImage1":
        this.filename1Error = true;
        break;
      case "ShareableImage2":
        this.filename2Error = true;
        break;
      case "ShareableImage3":
        this.filename3Error = true;
        break;
      case "ShareableImage4":
        this.filename4Error = true;
        break;
      default:
        break;
    }
  }

  getShareableImageButtonCss(imageItem: string) {
    let shareableImageButtonCssClass: string = this.shareableImageButtonEnabledCssClass;
    switch (imageItem) {
      case "ShareableImage1":
        if (this.filename1Error)
          shareableImageButtonCssClass = this.shareableImageButtonDisabledCssClass;
        break;
      case "ShareableImage2":
        if (this.filename2Error)
          shareableImageButtonCssClass = this.shareableImageButtonDisabledCssClass;
        break;
      case "ShareableImage3":
        if (this.filename3Error)
          shareableImageButtonCssClass = this.shareableImageButtonDisabledCssClass;
        break;
      case "ShareableImage4":
        if (this.filename4Error)
          shareableImageButtonCssClass = this.shareableImageButtonDisabledCssClass;
        break;
      default:
        break;
    }
    return shareableImageButtonCssClass;
  }

  getBadgeUrl(fileType, quality) {
    const encoder = new CustomHttpParamEncoder();
    var url = "/api/badge/CreateBadge?";
    url += "&clientId=" + this.clientId;
    url += "&engagementId=" + this.engagementId;
    url += "&imageType=" + fileType;
    url += "&quality=" + quality;
    url += "&token=" + encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    return url;
  }

  getBadgeZip() {
    const encoder = new CustomHttpParamEncoder();
    var url = "/api/badge/CreateBadgeZip?";
    url += "&clientId=" + this.clientId;
    url += "&engagementId=" + this.engagementId;
    url += "&token=" + encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    return url;
  }

  getImageUrl(index, fileType, quality) {
    const encoder = new CustomHttpParamEncoder();
    var url = "/api/badge/CreateShareableImage?";
    url += "index=" + index;
    url += "&clientId=" + this.clientId;
    url += "&engagementId=" + this.engagementId;
    url += "&imageType=" + fileType;
    url += "&quality=" + quality;
    url += "&token=" + encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    return url;
  }

  ngAfterViewInit(): void {
    // Refresh the twitter timeline
    (<any>window).twttr.widgets.load();
  }

  pad(num: number, size: number): string {
    let s = num + "";
    while (s.length < size) s = "0" + s;
    return s;
  }

  copyEmbedCode() {
    this.copyToClipboard('<a href="http://www.greatplacetowork.com/certified-company/' + this.clientId + '" title="Rating and Review" target="_blank"><img src="https://www.greatplacetowork.com/images/profiles/' + this.clientId + '/companyBadge.png" alt="Review" width="120" ></a>');
    event.preventDefault();
  }

  copyToClipboard(val: string) {
    const selBox = document.createElement('textarea');
    selBox.style.position = 'fixed';
    selBox.style.left = '0';
    selBox.style.top = '0';
    selBox.style.opacity = '0';
    selBox.value = val;
    document.body.appendChild(selBox);
    selBox.focus();
    selBox.select();
    document.execCommand('copy');
    document.body.removeChild(selBox);
  }


  sendBadgeEmail(): boolean {
    let isSuccess: boolean = true;
    let safeEmail = encodeURIComponent(this.toEmail);
    let safeReferrer = encodeURIComponent(this.referrer);
    let safeCelebrateLink = encodeURIComponent(this.celebrateLink);
    let safeUsageLink = encodeURIComponent(this.usageLink);
    let safePressReleaseLink = encodeURIComponent(this.pressReleaseLink);
    let safeReferrerEmail = encodeURIComponent(this.referrerEmail);
    this._httpClient.get<EmailToolkitResult>(this._baseUrl + 'api/Recognition/EmailToolkit?clientId=' + this.clientId + '&engagementId=' + this.engagementId + '&toEmailAddress=' + safeEmail + '&referrer=' + safeReferrer + '&celebratelink=' + safeCelebrateLink + '&usagelink=' + safeUsageLink + '&pressreleaselink=' + safePressReleaseLink + '&ccemail=' + safeReferrerEmail, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.isError) {
          console.log("error:result.errorMessage" + result.erorStr);
          isSuccess = false;
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
    return isSuccess;
  }

}

export class CustomHttpParamEncoder implements HttpParameterCodec {
  public encodeKey(key: string): string {
    return encodeURIComponent(key);
  }
  public encodeValue(value: string): string {
    return encodeURIComponent(value);
  }
  public decodeKey(key: string): string {
    return decodeURIComponent(key);
  }
  public decodeValue(value: string): string {
    return decodeURIComponent(value);
  }
}
