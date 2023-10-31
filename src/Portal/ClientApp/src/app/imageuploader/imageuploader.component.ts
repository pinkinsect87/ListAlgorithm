import { Component, OnInit, Inject, ViewChild } from '@angular/core';
import { UploadEvent, FileInfo } from '@progress/kendo-angular-upload';
import { HttpClient } from '@angular/common/http';


@Component({
  selector: 'app-imageuploader',
  templateUrl: './imageuploader.component.html',
  styleUrls: ['./imageuploader.component.scss'],
  
})
export class ImageuploaderComponent implements OnInit {
  private _kendoFiles: FileInfo[];
  private _baseUrl: string = "";
  private _filesSet: Set<File>;
  
  public uploadSaveUrl: string = "";
  public userPic: string;
  public items: string[] = [
    'Item 1', 'Item 2', 'Item 3', 'Item 4', 'Item 5', 'Item 6', 'Item 7', 'Item 8'
  ];

  constructor(private http: HttpClient,
    @Inject('BASE_URL') baseUrl: string) {
    this.uploadSaveUrl = "/api/files";
    this._baseUrl = baseUrl;
  }

  ngOnInit() {
  }

  successEventHandler(e) {
    this.userPic = e.response.body.fileName;
  }

  uploadEventHandler(e: UploadEvent) {
    console.log('Upload event: ', e);
    //e.headers = e.headers.append('Access-Control-Allow-Origin', '*');

    this._kendoFiles = e.files;
    this._filesSet = new Set<File>();

    for (var i = 0; i < this._kendoFiles.length; i++) {
      let rawFile: File = this._kendoFiles[i].rawFile;
      this._filesSet.add(rawFile);
    }

    this._filesSet.forEach(file => {
      // create a new multipart-form for every file
      const formData: FormData = new FormData();
      formData.append('file', file, file.name);

      // create a http-post request and pass the form
      // tell it to report the upload progress

      this.http.post<FormData>(this._baseUrl + "api/files", formData).subscribe(
        (data) => {
          console.log('Data: ', data);
        },
        (err) => {
          console.error(err);
        },
        () => {
          console.log('Post completed');
        });
    });

    //e.preventDefault(); //comment this in to see how files sent automatially aren't catched by api controller
  }
 
}

