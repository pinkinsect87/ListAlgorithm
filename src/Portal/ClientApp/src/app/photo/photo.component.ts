import { Component, OnInit, Inject, DoCheck, ViewEncapsulation, ViewChild } from '@angular/core';
import { UploadEvent, RemoveEvent, FileInfo, SelectEvent, FileRestrictions } from '@progress/kendo-angular-upload';
import { BackendService } from '../services/backend.service';
import { ClientPhoto, GetClientImageInfoResult, RepublishProfileResult, RepublishProfileRequest } from '../models/misc-models';
import { DataEvent, DragStartEvent, DragEndEvent, DragOverEvent, NavigateEvent } from '@progress/kendo-angular-sortable';
import { AuthService } from '../services/auth.service'
import { AppInsights } from 'applicationinsights-js';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router, ActivatedRoute } from '@angular/router';
import { DialogAction } from '@progress/kendo-angular-dialog';

@Component({
    selector: 'app-photo',
    templateUrl: './photo.component.html',
    encapsulation: ViewEncapsulation.None,
    styleUrls: ['./photo.component.scss']
})

export class PhotoComponent implements OnInit {
    @ViewChild('sortable', { static: true }) public sortable: any;
    private _kendoFiles: FileInfo[];
    private baseUrl: string = "";
    private _filesSet: Set<File>;
    private _interval;
    public clientPhotos: ClientPhoto[] = [];
    public isUploadDisabled: boolean = false;
    public disabledPhotoIndexes: Array<number> = [];
    public title: string = "Click to upload your profile images.";
    public uploadBaseSaveUrl: string = "/api/ClientImageUpload/SaveFile";
    public uploadSaveUrl: string = "";
    public instructions: string = "";
    public uploadRemoveUrl = 'removeUrl'; // should represent an actual API endpoint
    public draggableContainerClass: string = "draggable-container";
    public photoDragContainerClass: string = "photo-drag-container";
    public photoImageContainerClass: string = "photo-image-container";
    public imageContainerRemoveClass: string = "image-container-remove";
    public actionsLayout: string = 'normal';
    public dialogMessage: string = "";
    public dialogOpened = false;
    public dialogFinalMessage: string = "";
    public dialogFinalOpened = false;
    public dataLoaded: boolean = false;
    public cultureSurveyId: string = "";
    public clientId: number;
    public engagementid: number;
    public counter: { min: number, sec: number };
    public subCounter: number = 10;
    public waitingForRepublish: boolean = false;
    public dialogRepublishErrorOpened: boolean = false;
    public dialogRepublishSuccessOpened: boolean = false;
    public errorMessage = "";
    public showErrorMessage:boolean = false;
    private intervalId;

    public myActions = [
        { text: 'No' },
        { text: 'Yes', primary: true }
    ]

    constructor(private http: HttpClient,
        public bs: BackendService,
        private authService: AuthService,
        private router: Router,
        private route: ActivatedRoute,
        @Inject('BASE_URL') baseUrl: string) {
        this.baseUrl = baseUrl;

        route.params.subscribe(params => {
            this.clientId = params['clientId'];
        })

        }

        myRestrictions: FileRestrictions = {
            allowedExtensions: ['jpg', 'jpeg', 'png']
        };


    //Questions I will need to go to the CultureSurvey if only to get the state
    // Given a clientId Page needs to lookup the most recently submitted CB that resulted in a profile being published.
    // Order ECRV2's based on createdate. find the first with CB complete. Check that CultureSurvey to make sure that the state is complete

  ngOnInit() {
        AppInsights.trackPageView("In photo constructor. cliendId: " + this.clientId);
        this.getClientImages();
    }

    getClientImages() {
        this.http.get<GetClientImageInfoResult>(this.baseUrl + 'api/Portal/GetClientImageInfo?clientId=' + this.clientId, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
        if (!result.errorOccurred) {
            this.bs.setUpAppState(result);
            this.engagementid = result.engagementId;
            this.init();
        }
        else {
            if (result.errorMessage) {
                this.errorMessage = result.errorMessage;
                this.showErrorMessage = true;
                return;
            }
            else {
                this.router.navigate(["/error/general"]);
            }
        }
        }, error => {
            if (error.status === 401) {
              this.router.navigate(["/error/noaccess"]);
            }
            else {
              this.router.navigate(["/error/general"]);
            }
        });
    }

    init() {
        this.dataLoaded = true;
        this.uploadSaveUrl = this.uploadBaseSaveUrl + "?t=" + this.authService.getAuthorizationToken();
        this.clientPhotos = this.bs.getPhotos();
        this.isUploadDisabled = (this.clientPhotos != null && this.clientPhotos.length >= 7);

        // If the survey is submitted, disable the photo items in the sortable control
        if ((this.bs.surveyState.toLowerCase() == "complete") || (this.bs.surveyState.toLowerCase() == "submitting")) {
            for (var i = 0; i < this.clientPhotos.length; i++) {
                this.disabledPhotoIndexes.push(i);
            }
            this.draggableContainerClass = "draggble-container-disable";
            this.photoDragContainerClass = "photo-drag-container-hide";
            this.photoImageContainerClass = "photo-image-container-disable";
            this.imageContainerRemoveClass = "image-container-remove-hide";
        }

        this.instructions = "<p>Provide at least 5 and no more than 7 photos to be included in your Certification Profile.</p>";
        this.instructions += "<p>If you win placement on a list, we use 'Photo 1' to represent your organization. Please ensure this is a professional photo that you will be proud to share on greatplacetowork.com, our social media channels, and Fortune.com.</p>";
        this.instructions += "<p><b>Photo Requirements</b></p>";
        this.instructions += "<p><b>&bull;</b>&nbsp;&nbsp;High resolution (at least 1,000 pixels wide)</p>";
        this.instructions += "<p><b>&bull;</b>&nbsp;&nbsp;Horizontal/landscape format; NOT vertical/portrait</p>";
        this.instructions += "<p><b>&bull;</b>&nbsp;&nbsp;Clear, NOT blurry</p>";
        this.instructions += "<p><b>&bull;</b>&nbsp;&nbsp;A photo&nbsp;&mdash;&nbsp;not a designed image. (No logos, screen shots, drawings, advertising, etc.)</p>";
        this.instructions += "<p><b>&bull;</b>&nbsp;&nbsp;Your photo will stand out best if it tells a story and shares something about what makes your culture unique! Lots of other organizations will share photos of a large group shot of their people. Can you capture a candid moment of what it feels like to work in your organization?</p>";
        this.instructions += "<p>By uploading these photographs, you are indicating that you are the copyright owner and giving us permission to use these images and share it with media partners.</p>";
        this.instructions += "<p><b>Certification Profile</b></p><p>This will be published on your Certification Profile.</p><p>Click here to see examples of live <a target='_blank' href='https://www.greatplacetowork.com/certified-companies/'>Certification Profiles</a>.</p>";
    }

    isPreviewDisabled() {
        return this.clientPhotos.length == 0;
    }

    public GetUri(item) {
      var url = "api/Portal/GetClientAsset?";
      url += "fileName=" + item.fileName;
      return url;
    }

    onPreviewButtonClick() {
        this.bs.slideImageIndex = 0;
        let photos = this.bs.getPhotos();

        var url = "api/Portal/GetClientAsset?";
        url += "fileName=" + photos[this.bs.slideImageIndex].fileName;
        this.bs.photoFileName = url;

        this.bs.photoCaption = photos[this.bs.slideImageIndex].caption;
        this.bs.showImagePreview = true;
    }

    public saveClientPhotos() {
        //if ((this.bs.surveyState.toLowerCase() != "complete") && (this.bs.surveyState.toLowerCase() != "submitting")) {
            this.bs.saveClientPhotos(this.clientPhotos, true);
            //this.bs.Validation();
            //if (this.bs.myErrors.length == 0) {
            //    this.bs.opened = false;
            //}//
        //}
    }

    onFocusOut() {
        this.saveClientPhotos();
    }

  onMouseUp(item) {
    let fileNameToDelete = item.fileName;
      let indexToRemove = this.clientPhotos.findIndex(d => d.fileName == item.fileName); //find index in your array

        let clientPhotosAfterRemoval = [];
        let index: number = 0;

        for (var i = 0; i < this.clientPhotos.length; i++) {
            if (i != indexToRemove) {
                let clientPhoto: ClientPhoto = this.clientPhotos[i];
                let newClientPhoto: ClientPhoto = new ClientPhoto();
                newClientPhoto.fileName = clientPhoto.fileName;
                newClientPhoto.caption = clientPhoto.caption;
                newClientPhoto.primary = clientPhoto.primary;
                clientPhotosAfterRemoval[index++] = newClientPhoto;
            }
        }

        this.clientPhotos = clientPhotosAfterRemoval;

        this.saveClientPhotos();

        this.isUploadDisabled = (this.clientPhotos != null && this.clientPhotos.length >= 7);

        this.bs.deleteClientImageFromBlobStorage(fileNameToDelete);
    }

    successEventHandler(e) {
        // Don't allow more then 7 photos to be added
        if (this.clientPhotos != null && this.clientPhotos.length >= 7) {
            return;
        }

        let fileName: string = e.response.body.fileName;
        let blobName: string = e.response.body.blobName;

        //Skip adding photos if a photo doesn't validate
        if (fileName.match("VALIDATION_ERROR")) {
          this.router.navigate(["/error/invalidfile"]);
          return;
        }

        let clientPhotosAfterAddition = [];

        let newClientPhoto: ClientPhoto = new ClientPhoto();
        newClientPhoto.fileName = fileName;
        newClientPhoto.caption = "";
        newClientPhoto.primary = false;

        clientPhotosAfterAddition[0] = newClientPhoto;

        for (var i = 0; i <= this.clientPhotos.length - 1; i++) {
            if (i <= 5) {
                let clientPhoto: ClientPhoto = new ClientPhoto();
                clientPhoto.fileName = this.clientPhotos[i].fileName;
                clientPhoto.caption = this.clientPhotos[i].caption;
                clientPhoto.primary = this.clientPhotos[i].primary;
                clientPhotosAfterAddition[i + 1] = clientPhoto;
            }
        }

        this.clientPhotos = clientPhotosAfterAddition;
        this.saveClientPhotos();
        this.isUploadDisabled = (this.clientPhotos != null && this.clientPhotos.length >= 7);
    }

    uploadEventHandler(e: UploadEvent) {
      e.data = {
        cid: this.clientId,
        eid: this.engagementid
      };
    }

    removeEventHandler(e: RemoveEvent) {
        e.data = {
            description: 'File remove'
        };
    }

    public onDragEnd(e: DragEndEvent): void {
        this.saveClientPhotos();
    }

    closeImagePreview() {
        this.bs.showImagePreview = false;
        this.bs.slideImageIndex = 0;
    }

    closeLogoPreview() {
        this.bs.showLogoPreview = false;
    }

    setNextImage(direction: number) {
        let photos = this.bs.getPhotos();
        this.bs.slideImageIndex += direction;
        if (this.bs.slideImageIndex < 0) {
            this.bs.slideImageIndex = photos.length - 1;
        }
        if (this.bs.slideImageIndex > photos.length - 1) {
            this.bs.slideImageIndex = 0;
        }
        var url = "api/Portal/GetClientAsset?";
        url += "fileName=" + photos[this.bs.slideImageIndex].fileName;
        this.bs.photoFileName = url;
        this.bs.photoCaption = photos[this.bs.slideImageIndex].caption;
    }

    onButtonClick() {
        var valText: string = this.ImagesValidationText();
        if ((valText.length) > 0) {
            this.dialogMessage = valText;
            this.dialogOpened = true;
        }
        else {
            this.bs.saveClientPhotos(this.clientPhotos, true);
            this.requestProfileRepublish();
        }
    }

    ImagesValidationText() {
        var result: string = "";
        var missingCaptionFound: boolean = false;

        if (this.clientPhotos.length < 5) {
            result += "There is a minimum of 5 profile images required.";
        }

        for (let thisPhoto of this.clientPhotos) {
            if (thisPhoto.caption.length == 0) {
                missingCaptionFound = true;
                break;
            }
        }

        if (missingCaptionFound) {
            result += "Captions are required for each profile image.";
        }

        return result;
    }

    public action(status) {
        this.dialogOpened = false;
    }

    public close(component) {
        this[component + 'Opened'] = false;
    }

    requestProfileRepublish() {
        this.dialogFinalOpened = true;

        let republishProfileRequest = new RepublishProfileRequest;
        republishProfileRequest.clientId = this.clientId;
        republishProfileRequest.engagementId = this.engagementid;
        this.http.post<RepublishProfileResult>(this.baseUrl + 'api/Portal/RepublishProfile', republishProfileRequest, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
            if (!result.isError) {
               // this.startTimer();
            }
            else {
                this.dialogFinalOpened = false;
                this.dialogRepublishErrorOpened = true;
            }
        }, error => {
                console.error(error)
                this.dialogFinalOpened = false;
                this.dialogRepublishErrorOpened = true;
        });
    }

    //startTimer() {
    //    this.counter = { min: 7, sec: 0 } 
    //    let intervalId = setInterval(() => {
    //        if (this.counter.sec - 1 == -1) {
    //            this.counter.min -= 1;
    //            this.counter.sec = 59
    //        }
    //        else this.counter.sec -= 1

    //        this.subCounter -= 1;
    //        if (this.subCounter == 0) { // Check for completion every x seconds
    //            this.subCounter = 15;
    //            this.getProfilePublishStatus();
    //        }

    //        if (this.counter.min === 0 && this.counter.sec == 0){
    //            clearInterval(intervalId);
    //            this.dialogFinalOpened = false;
    //            this.dialogRepublishErrorOpened = true;
    //        }
    //    }, 1000)
    //    this.intervalId = intervalId;
    //}

    getProfilePublishStatus() {
        let profilePublishStatusRequest = new RepublishProfileRequest;
        profilePublishStatusRequest.clientId = this.clientId;
        profilePublishStatusRequest.engagementId = this.engagementid;
        this.http.post<RepublishProfileResult>(this.baseUrl + 'api/Portal/GetProfilePublishStatus', profilePublishStatusRequest, { headers: this.authService.getRequestHeaders() }).subscribe(result => {
            if (!result.isError) {
                if (result.reviewPublishStatus == "Success") {
                    clearInterval(this.intervalId);
                    this.dialogFinalOpened = false;
                    this.dialogRepublishSuccessOpened = true;
                }
            }
            else {
                clearInterval(this.intervalId);
                this.dialogFinalOpened = false;
                this.dialogRepublishErrorOpened = true;
            }
        }, error => {
            console.error(error);
            clearInterval(this.intervalId);
            this.dialogFinalOpened = false;
            this.dialogRepublishErrorOpened = true;
        });
    }

    viewProfile() {
        this.bs.navigateExtNewWindow(this.bs.getGPTWWebSiteBaseUrl() + "certified-company/" + this.clientId);
    }
}
