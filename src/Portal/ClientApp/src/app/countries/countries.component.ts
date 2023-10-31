import { Component, OnInit, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-countries',
  templateUrl: './countries.component.html',
  styleUrls: ['./countries.component.scss']
})
export class CountriesComponent implements OnInit {
  @Output() numberOfEmployees: EventEmitter<number> = new EventEmitter<number>();
  @Output() countryCode: EventEmitter<string> = new EventEmitter<string>();
  public ccode: string = "";
  public numEmp: number;
  emitCountryCode() {
    this.countryCode.emit(this.ccode);
  }
  emitNumEmp() {
    this.numberOfEmployees.emit(this.numEmp)
  }

  constructor() { }

  ngOnInit() {
  }

}
