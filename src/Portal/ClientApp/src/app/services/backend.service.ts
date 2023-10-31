import { Injectable, Inject, EventEmitter } from '@angular/core';
import { HttpClient, HttpParameterCodec } from '@angular/common/http';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service'
import { GridSettings, FindCurrentEngagementInfoResult, HeaderInfo, ClientPhoto, ErrorHighlights, GetClientImageInfoResult, SaveClientImagesRequest, DeleteClientImageRequest, ClientConfiguration, GetEngagementInfoResult, HeaderNotifyEvent, UserEventName, PostUserEventResult} from '../models/misc-models';

const getCircularReplacer = () => {
  const seen = new WeakSet();
  return (key, value) => {
    if (typeof value === "object" && value !== null) {
      if (seen.has(value)) {
        return;
      }
      seen.add(value);
    }
    return value;
  };
};

@Injectable({
    providedIn: 'root'
})

export class BackendService {
    private router: Router;
    private newECRResult = "";
    public newECREngagementId: number;
    public baseUrl: string;
    private httpClient: HttpClient;
    private counter = 0;
    public config: ClientConfiguration;
    public companyName = "";
    public onChange: EventEmitter<HeaderNotifyEvent> = new EventEmitter<HeaderNotifyEvent>();
    private ERROR_ID_FAILED_TO_FIND_ECRV2 = 3;

    // Might not be needed
    surveyId: string;

    data: object;
    postAPIUrl: string;
    loading: boolean;
    clientPhotos: ClientPhoto[];
    public showImagePreview = false;
    public slideImageIndex = 0;
    public photoFileName = "";
    public photoCaption = "";
    public surveyState: string;
    engagementId = "";
    status = "";
    logoFileName: string;
    headerInfo: HeaderInfo = new HeaderInfo;

    //Validation Related Properties
    _myErrors: ErrorHighlights[];
    public noErrors: boolean;
    public opened: boolean;

    public get myErrors(): ErrorHighlights[] {
        return this._myErrors;
    }

    // REMOVE
  public showLogoPreview = false;

    constructor(private authService: AuthService, http: HttpClient, private routerRef: Router, @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;
        this.httpClient = http;
        this.config = authService.config;
        this.router = routerRef;
    }

    ngInit() {

    }

    public get<T>(token: string): T {
      const settings = localStorage.getItem(token);
      return settings ? JSON.parse(settings) : settings;
    }

    public set<T>(token: string, gridConfig: GridSettings): void {
      localStorage.setItem(token, JSON.stringify(gridConfig, getCircularReplacer()));
  }

    clearHeaderCompanyName() {
      this.headerInfo.clientName = "";
  }

  public viewEmail(id: string): void {
    var encoder = new CustomHttpParamEncoder();
    let token = encoder.encodeKey(this.authService.getAuthorizationHeaderTokenValueOnly());
    var url = "/api/Portal/GetEmail?id=" + id + "&token=" + token;
    this.navigateExtNewWindow(url);
  }

    // This function is called when each component (that needs Header support) starts up. The Component passes in cid or eid
    // which is compared to what was previously stored. If a change is detected a call is made to a controller to retreive
    // the cid/eid/clientName. This data is required by the header to properly function.
    setHeaderDependencies(name: string, value: number) {
        let update = false;
        let propertyName = "";
        let property = "";

        // If the current value hasn't been initialized OR it doesn't match what was passed in the we need to update
        if (name.toLowerCase() == "clientid" && (this.headerInfo.clientId == -1 || value != this.headerInfo.clientId)) {
            propertyName = "clientId";
            property = value.toString();
            update = true;
        }

        // If the current value hasn't been initialized OR it doesn't match what was passed in the we need to update
        if (name.toLowerCase() == "engagementid" && (this.headerInfo.engagementId == -1 || value != this.headerInfo.engagementId)) {
            propertyName = "engagementId";
            property = value.toString();
            update = true;
        }

        if (update) {
            this.httpClient.get<FindCurrentEngagementInfoResult>(this.baseUrl + 'api/Portal/FindCurrentEngagementInfo?propertyName=' + propertyName + '&property=' + property, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
                if (result) {
                  if (result.errorOccurred) {
                    if (result.errorId == this.ERROR_ID_FAILED_TO_FIND_ECRV2) {
                            this.router.navigate(["/error/noengagement"]);
                          }
                          else {
                            this.router.navigate(["/error/general"]);
                          }
                    }
                    else {
                        if (result.clientName != this.headerInfo.clientName) {
                            this.headerInfo.clientName = result.clientName;
                        }
                        if (result.clientId != this.headerInfo.clientId) {
                            this.headerInfo.clientId = result.clientId
                        }
                        if (result.engagementId != this.headerInfo.engagementId) {
                            this.headerInfo.engagementId = result.engagementId
                        }
                    }
                }
                else {
                    this.router.navigate(["/error/general"]);
                }
            }, error => {
                console.error(error);
                this.router.navigate(["/error/general"]);
            }
            );
        }
    }

    getGPTWWebSiteBaseUrl() {
        return this.config.GPTWWebSiteBaseUrl;
    }

    getZendeskAPIUrl(){
        return this.config.ZendeskAPIUrl;
    }

    saveNewECRResult(newECRResult: string) {
        this.newECRResult = newECRResult;
  }
  saveNewECREngagement(engagementid: number) {
    this.newECREngagementId = engagementid;
  }
    getNewECRResult() {
        return this.newECRResult;
    }

    navigateExt(url: string) {
        (this.authService.nativeWindow as any).location.href = url;
        //(this.authService.nativeWindow as any).open(url, "_blank");
    }
    navigateExtNewWindow(url: string) {
        //(this.authService.nativeWindow as any).location.href = url;
        (this.authService.nativeWindow as any).open(url, "_blank");
    }


    //getOverviewRouting() {
    //    let route: string = "/";
    //    if (this.engagementId.length > 0) {
    //        route = "/certify/engagement/" + this.engagementId;
    //        if (this.status.length > 0) {
    //            route = route + "/" + this.status;
    //        }
    //    }
    //    return route;
    //}

    setEngagementIdAndStatus(engagementId:string, status:string) {
        this.engagementId = engagementId;
        this.status = status;
    }

    setUpAppState(dto: GetClientImageInfoResult) {
        this.surveyId = dto.surveyId;
        this.surveyState = dto.surveyStatus;
        this.logoFileName = dto.logoFileName;
        this.clientPhotos = [];

        if (dto.clientPhotos != undefined) {
            for (var i = 0; i < dto.clientPhotos.length; i++) {
                let clientPhoto: ClientPhoto = dto.clientPhotos[i];
                let newClientPhoto: ClientPhoto = new ClientPhoto();
              newClientPhoto.fileName = clientPhoto.fileName;

                newClientPhoto.caption = clientPhoto.caption;
                newClientPhoto.primary = clientPhoto.primary;
                this.clientPhotos[i] = newClientPhoto;
            }
        }
    }

  deleteClientImageFromBlobStorage(fileName: string) {
        this.postAPIUrl = this.baseUrl + 'api/Portal/DeleteClientImageFromBlobStorage';
        this.loading = true;

        let myPost: DeleteClientImageRequest = {
            cultureSurveyId: this.surveyId,
            fileName: fileName
        }

        this.httpClient.post(this.postAPIUrl, myPost, { headers: this.authService.getRequestHeaders() })
            .subscribe(data => {
                this.data = data;
                this.loading = false;
            });
    }


    saveClientPhotos(photos: ClientPhoto[], saveRemotely) {
        this.clientPhotos = photos; // Save Locally
        if (saveRemotely)
          this.saveClientImages(); // Save Remotely
    }

    saveClientImages() {

        this.postAPIUrl = this.baseUrl + 'api/Portal/SaveClientImages';
        this.loading = true;

        let myPost: SaveClientImagesRequest = {
            cultureSurveyId: this.surveyId,
            clientPhotos: this.clientPhotos,
            logoFileName: this.logoFileName
        }

        this.httpClient.post(this.postAPIUrl, myPost, { headers: this.authService.getRequestHeaders() })
            .subscribe(data => {
                this.data = data;
                this.loading = false;
            });
    }


    getPhotos() {
        return this.clientPhotos;
    }


    Validation() {
        console.log("In validation");
        this._myErrors = [];
        //this.resetErrorColor("company-logo");
        //this.resetErrorColor("company-images");

        this.ImagesValidation();

        //if (this._myErrors.length == 0) {
        //    this.CrossFieldValidation();
        //}

        //if (this._myErrors.length == 0) {
        //    this.noErrors = true;
        //    this.opened = false;
        //}
        //else {
        //    this.noErrors = false;
        //}
    }

    ImagesValidation() {
        var result: string = "";

        if (this.clientPhotos.length < 5) {
            result += "Required: At least 5 profile images.";

            //let newError = new ErrorHighlights;
            //newError.errorText = "Required: At least 5 profile images.";
            //newError.highlightVars = ["company-images"];
            //newError.hoverText = "";
            //this._myErrors.push(newError);
        }
        var missingCaptionFound: boolean = false;

        for (let thisPhoto of this.clientPhotos) {
            if (thisPhoto.caption.length == 0) {
                missingCaptionFound = true;
                break;
            }
        }

        if (missingCaptionFound)
            result += "Required: Captions for each profile images.";

    }

    //isValidEmail(email: string): boolean {

    //    var emailValidator = /^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    //    let result = email.match(emailValidator);
    //    if (!result)
    //        return false;
    //    else
    //        return true;
    //}

  public postUserEvent(name: UserEventName) {
    const cid = this.headerInfo.clientId;
    const eid = this.headerInfo.engagementId;
    this.httpClient.get<PostUserEventResult>(this.baseUrl + 'api/Portal/PostUserEvent?userEventEnumName=' + name + "&cid=" + cid + "&eid=" + eid, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
      if (result) {
        if (result.isError) {
          this.router.navigate(["/error/general"]);
        }
        else {
          return;
        }
      }
      else {
        this.router.navigate(["/error/general"]);
      }
    }, error => {
      console.error(error);
      this.router.navigate(["/error/general"]);
    }
    );
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

