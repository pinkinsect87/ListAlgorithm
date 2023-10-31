import { Component, Input, Output, EventEmitter, AfterViewInit } from '@angular/core';
import { CompositeFilterDescriptor, distinct, filterBy, FilterDescriptor } from '@progress/kendo-data-query';
import { FilterService } from '@progress/kendo-angular-grid';

@Component({
  selector: 'multicheck-filter',
  templateUrl: './multicheck-filter.component.html',
  styleUrls: ['./multicheck-filter.component.scss']
})
export class MultiCheckFilterComponent implements AfterViewInit {
  @Input() public isPrimitive: boolean;
  @Input() public currentFilter: CompositeFilterDescriptor;
  @Input() public data;
  @Input() public textField;
  @Input() public valueField;
  @Input() public filterService: FilterService;
  @Input() public field: string;
  @Output() public valueChange = new EventEmitter<number[]>();

  public currentData: any;
  public showFilter = false;
  private value: any[] = [];

  protected textAccessor = (dataItem: any) => this.isPrimitive ? dataItem : dataItem[this.textField];
  protected valueAccessor = (dataItem: any) => this.isPrimitive ? dataItem : dataItem[this.valueField];

  public ngAfterViewInit() {
    this.currentData = this.data;
    this.value = this.currentFilter.filters.map((f: FilterDescriptor) => f.value);

    this.showFilter = typeof this.textAccessor(this.currentData[0]) === 'string';
  }

  public isItemSelected(item) {
    return this.value.some(x => x === this.valueAccessor(item));
  }

  public onSelectionChange(item, li) {
    if (this.value.some(x => x === item)) {
      this.value = this.value.filter(x => x !== item);
    } else {
      this.value.push(item);
    }

    this.filterService.filter({
      filters: this.value.map(value => ({
        field: this.field,
        operator: 'eq',
        value
      })),
      logic: 'or'
    });

    this.onFocus(li);
  }

  public onInput(e: any) {
    this.currentData = distinct([
      ...this.currentData.filter(dataItem => this.value.some(val => val === this.valueAccessor(dataItem))),
      ...filterBy(this.data, {
        operator: 'contains',
        field: this.textField,
        value: e.target.value
      })],
      this.textField
    );
  }

  public onFocus(li: any): void {
    const ul = li.parentNode;
    const below = ul.scrollTop + ul.offsetHeight < li.offsetTop + li.offsetHeight;
    const above = li.offsetTop < ul.scrollTop;

    // Scroll to focused checkbox
    if (below || above) {
      ul.scrollTop = li.offsetTop;
    }
  }

  public getIconName(data: string): string {
    switch (data) {
      case "Pending": {
        return "panorama_fish_eye";
        break;
      }
      case "Not Certified": {
        return "highlight_off";
        break;
      }
      case "Certified": {
        return "check_circle_outline";
        break;
      }
      case "Survey In Progress": {
        return "info";
        break;
      }
      case "Survey Closed": {
        return "start";
        break;
      }
      default: {
        return "continue";
        break;
      }
    }
  }


}
