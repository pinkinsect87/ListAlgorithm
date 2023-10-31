import { Component, Inject, OnInit } from '@angular/core';
import { BackendService } from '../services/backend.service';
import { FilterService, PageSizeItem, PageChangeEvent } from '@progress/kendo-angular-grid';
import { filterBy, FilterDescriptor, CompositeFilterDescriptor, DataResult, process, State } from '@progress/kendo-data-query';

export class ECRV2Info {
  cid: number;
  eid: number;
  createdate: Date;
  cname: string;
  tistatus: string;
  cbstatus: string;
  castatus: string;
  cstatus: string;
  //tilink: string;
  //cblink: string;
  //calink: string;
  //profileAvailable: boolean;
}

export interface GridSettings {
  columnsConfig: ColumnSettings[];
  state: State;
  pageSize: number;
  pageSizes: PageSizeItem[];
  gridData?: DataResult;
}

export interface ColumnSettings {
  field: string;
  title?: string;
  hidden: boolean;
  filter?: 'string' | 'numeric' | 'date' | 'boolean';
  format?: string;
  width?: number;
  _width?: number;
  filterable: boolean;
  orderIndex?: number;
}

export const DefaultGridSettings: GridSettings = {
  pageSize: 100,
  pageSizes: [{ text: '100', value: 100 }, { text: '500', value: 500 }, { text: '1000', value: 1000 }, { text: 'All', value: 'all' }],
  state: {
    skip: 0,
    take: 100,
    // Initial filter descriptor
    filter: {
      logic: 'and',
      filters: []
    }
  },
  gridData: null,
  columnsConfig: [{
    field: 'cname',
    title: 'Company Name',
    hidden: false,
    filterable: true,
    _width: 160
  }, {
    field: 'cid',
    title: 'ClientId',
    hidden: true,
    filter: 'numeric',
    format: '{0:c}',
    _width: 100,
    filterable: true
  }, {
    field: 'eid',
    title: 'EngagementId',
    hidden: true,
    filter: 'numeric',
    format: '{0:c}',
    _width: 100,
    filterable: true
  }, {
    field: 'createdate',
    title: 'Create Date',
    hidden: false,
    filter: 'date',
    format: '{0:d}',
    _width: 130,
    filterable: true
  }, {
    field: 'tistatus',
    title: 'TI Status',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'cbstatus',
    title: 'CB Status',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'castatus',
    title: 'CA Status',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'cstatus',
    title: 'Certification',
    hidden: false,
    _width: 70,
    filterable: true
  }]
};

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

const flatten = filter => {
  const filters = (filter || {}).filters;
  if (filters) {
    return filters.reduce((acc, curr) => acc.concat(curr.filters ? flatten(curr) : [curr]), []);
  }
  return [];
};

const gridSettingsName: string = "GridSettingsNameV98";

@Component({
  selector: 'app-gptwemployee',
  templateUrl: './gptwemployee.component.html',
  styleUrls: ['./gptwemployee.component.scss']
})

export class GptwemployeeComponent implements OnInit {
  public data: any[] = [];
  public gridSettings = DefaultGridSettings;
  public filter: CompositeFilterDescriptor;

  public tiStatusSelectedValue: Array<string> = [];
  public caStatusSelectedValue: Array<string> = [];
  public cbStatusSelectedValue: Array<string> = [];
  public cStatusSelectedValue: Array<string> = [];
  public tiStatusItems: Array<string> = ['Created', 'Setup in Progress', 'Ready to Launch', 'Survey in Progress', 'Survey Closed', 'Data Transferred', 'Data Loaded', 'Abandoned', 'Opted - Out'];
  public caStatusItems: Array<string> = ['Created', 'In Progress', 'Completed', 'Abandoned', 'Opted - Out'];
  public cbStatusItems: Array<string> = ['Created', 'In Progress', 'Completed', 'Abandoned', 'Opted - Out'];
  public cStatusItems: Array<string> = ['Pending', 'Certified', 'Not Certified'];

  private statusFilter: any[] = [];

  constructor() {
  
  }

  ngOnInit() {
    const names = ["adamant", "adroit", "amatory", "animistic", "antic", "arcadian", "baleful", "bellicose", "bilious", "boorish", "calamitous", "caustic", "cerulean", "comely", "concomitant", "contumacious", "corpulent", "crapulous", "defamatory", "didactic", "dilatory", "dowdy", "efficacious", "effulgent", "egregious", "endemic", "equanimous", "execrable", "fastidious", "feckless", "fecund", "friable", "fulsome", "garrulous", "guileless", "gustatory", "heuristic", "histrionic", "hubristic", "incendiary", "insidious", "insolent", "intransigent", "inveterate", "invidious", "irksome", "jejune", "jocular", "judicious", "lachrymose", "limpid", "loquacious", "luminous", "mannered", "mendacious", "meretricious", "minatory", "mordant", "munificent", "nefarious", "noxious", "obtuse", "parsimonious", "pendulous", "pernicious", "pervasive", "petulant", "platitudinous", "precipitate", "propitious", "puckish", "querulous", "quiescent", "rebarbative", "recalcitant", "redolent", "rhadamanthine", "risible", "ruminative", "sagacious", "salubrious", "sartorial", "sclerotic", "serpentine", "spasmodic", "strident", "taciturn", "tenacious", "tremulous", "trenchant", "turbulent", "turgid", "ubiquitous", "uxorious", "verdant", "voluble", "voracious", "wheedling", "withering", "zealous"];
    const data = [];
    for (let i = 0; i < 10; i++) {
      let newObj = new ECRV2Info();
      let year = Math.floor(Math.random() * 30) + 1990;
      let month = Math.floor(Math.random() * 12) + 1;
      let day = Math.floor(Math.random() * 31) + 1;
      newObj.createdate = new Date(year + "-" + month + "-" + day);
      newObj.tistatus = this.tiStatusItems[i % this.cbStatusItems.length];
      newObj.cbstatus = this.cbStatusItems[i % this.cbStatusItems.length];
      newObj.castatus = this.caStatusItems[i % this.caStatusItems.length];
      newObj.cstatus = this.cStatusItems[i % this.cStatusItems.length];
      if (i < 100)
        newObj.cname = names[i];
      else
        newObj.cname = "z" + names[i-100];
      newObj.eid = 50000 + Math.floor(Math.random() * 9999);
      newObj.cid = 100000 + Math.floor(Math.random() * 50000);
      data.push(newObj);
    }
    this.data = data;

    let total = data.length;

    const gridSettings: GridSettings = this.get(gridSettingsName);
    if (gridSettings !== null) {
      this.gridSettings = this.mapGridSettings(gridSettings);
      this.writeFilters("Loaded from localstorage:", this.gridSettings.state.filter, 0);
    }
    else {
      console.log("No gridSettings loaded from localstorage!")
    }
    this.gridSettings.gridData = process(this.data, this.gridSettings.state);
  }

  private writeFilters = (str: string, descriptor: any, level:number) => {
    const filters = descriptor.filters || [];
    if (level == 0) {
      console.log(str);
      console.log("writeFilters:start")
    }
    filters.forEach(filter => {
      if (filter.filters) {
        console.log("logic:" + filter.logic)
        this.writeFilters(str, filter, (level+1));
      }
      else{
        //if (filter.field === 'createdate' && filter.value) {
        //  filter.value = new Date(filter.value);
        //}
        console.log("filter[" + level + "] (field,operator,value)=" + filter.field + "," + filter.operator + "," + filter.value);
      }
    });
    if (level == 0)
      console.log("writeFilters:end")
  }

  public resetGridSettingsToDefaultAndSave() {
    this.gridSettings.state.filter = {
      logic: 'and',
      filters: []
    };

    this.dataStateChange(this.gridSettings.state);
  }

  public getDate(strCreateDate: string) {
    const createDate: Date = new Date(strCreateDate);
    return (createDate.getMonth() + 1) + "/" + createDate.getDate() + "/" + createDate.getFullYear();
  }

  public dataStateChange(state: State): void {
    console.log("dataStateChange called");
    this.gridSettings.state = state;
    this.gridSettings.gridData = process(this.data, state);
    this.saveGrid();
  }

  public pageChange({ skip, take }: PageChangeEvent): void {
    this.gridSettings.state.skip = skip;
    this.gridSettings.pageSize = take;
    this.gridSettings.state.take = take;
    this.gridSettings.gridData = process(this.data, this.gridSettings.state);
    this.saveGrid();
  }

  public onReorder(e: any): void {
    // Find index of item to extract from columnsConfig
    const fieldToExtract = e.column.field;
    let reorderedColumn = null;
    for (let i = 0; i < this.gridSettings.columnsConfig.length; i++) {
      // skip any hidden columns
      if (this.gridSettings.columnsConfig[i].hidden)
        continue;
      if (this.gridSettings.columnsConfig[i].field === fieldToExtract) {
        reorderedColumn = this.gridSettings.columnsConfig.splice(i, 1);
        break;
      }
    }

    // Find where to insert reorderedColumn
    let indexOfColumnToMatch = 0;
    for (let i = 0; i <= this.gridSettings.columnsConfig.length; i++) {
      // skip any hidden columns
      if (i < this.gridSettings.columnsConfig.length && this.gridSettings.columnsConfig[i].hidden)
        continue;
      if (indexOfColumnToMatch === e.newIndex) {
        this.gridSettings.columnsConfig.splice(i, 0, ...reorderedColumn);
        break;
      }
      indexOfColumnToMatch++;
    }

    this.saveGrid();
  }

  public onResize(e: any): void {
    e.forEach(item => {
      this.gridSettings.columnsConfig.find(col => col.field === item.column.field).width = item.newWidth;
    });
    this.saveGrid();
  }

  public onColumnVisibilityChange(e: any): void {
    e.columns.forEach(column => {
      this.gridSettings.columnsConfig.find(col => col.field === column.field).hidden = column.hidden;
    });
    this.saveGrid();
  }

  public mapGridSettings(gridSettings: GridSettings): GridSettings {
    const state = gridSettings.state;
    this.mapDateFilter(state.filter);

    return {
      state,
      pageSize: gridSettings.pageSize,
      pageSizes: gridSettings.pageSizes,
      columnsConfig: gridSettings.columnsConfig.sort((a, b) => a.orderIndex - b.orderIndex),
      gridData: process(this.data, state)
    };
  }

  private saveGrid(): void {
    const gridConfig = {
      pageSize: this.gridSettings.pageSize,
      pageSizes: this.gridSettings.pageSizes,
      columnsConfig: this.gridSettings.columnsConfig,
      state: this.gridSettings.state
    };

    this.set(gridSettingsName, gridConfig);
    this.writeFilters("Saved to localstorage:", this.gridSettings.state.filter, 0);
  }

  private mapDateFilter = (descriptor: any) => {
    const filters = descriptor.filters || [];
    
    filters.forEach(filter => {
      if (filter.filters) {
        this.mapDateFilter(filter);
      } else if (filter.field === 'createdate' && filter.value) {
        filter.value = new Date(filter.value);
      }
    });
  }

  mapColumnIndexToColumnName(columnIndex: number): string {
    let index = 0;
    for (let i = 0; i < this.gridSettings.columnsConfig.length; i++) {
      if (this.gridSettings.columnsConfig[i].hidden)
        continue;
      if (index === columnIndex)
        return this.gridSettings.columnsConfig[i].field;
      index++;
    }
    return "";
  }

  //fires each time the value is changed— when the component is blurred or the value is cleared
  //through the Clear button(see example).When the value of the component is programmatically changed to ngModel
  //or formControl through its API or form binding, the valueChange event is not triggered
  //because it might cause a mix-up with the built -in valueChange mechanisms of the ngModel or formControl bindings.
  public statusFilters(filter: CompositeFilterDescriptor): FilterDescriptor[] {
    console.log("statusFilters called");
    this.statusFilter.splice(
      0, this.statusFilter.length,
      ...flatten(filter).map(({ value }) => value)
    );

    this.saveGrid();
    return this.statusFilter;
  }

  //Fires each time the user types in the input field. You can filter the source based on the passed filtration value.
  public statusChange(fieldName: string, values: any[], filterService: FilterService): void {
    console.log("statusChange called.fieldName=" + fieldName + ",values:" + values.join(","));

    filterService.filter({
      filters: values.map(value => ({
        field: fieldName,
        operator: 'eq',
        value
      })),
      logic: 'or'
    });

    this.saveGrid();
  }

  public get<T>(token: string): T {
    const settings = localStorage.getItem(token);
    return settings ? JSON.parse(settings) : settings;
  }

  public set<T>(token: string, gridConfig: GridSettings): void {
    localStorage.setItem(token, JSON.stringify(gridConfig, getCircularReplacer()));
  }


}
