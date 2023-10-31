import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-image-gallery',
  templateUrl: './image-gallery.component.html',
  styleUrls: ['./image-gallery.component.scss']
})

export class ImageGalleryComponent implements OnInit {
  @Input() images: string;
  @Input() width: string;
  @Input() height: string;
  //public width = '319px';
  //public height = '540px';
  public items: string[] = [];

  constructor() {
  }

  ngOnInit() {
    let images = this.images.split(",");
    let i: number;
    for (i = 0; i < images.length; i++) {
      let item: string = images[i];
      this.items.push(item);
    }
  }
}
