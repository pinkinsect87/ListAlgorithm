import { State, DataResult } from "@progress/kendo-data-query";
import { PageSizeItem } from '@progress/kendo-angular-grid';

export class GetCertAPPInfoResult {
  errorOccurred: boolean;
  errorMessage: string;
  errorId: number;
  openApplications: CertificationApplication[];
  submittedApplications: CertificationApplication[];
}

export class CertificationApplication {
  engagementId: number;
  applicationType: number;
  cbStatus: number;
  caStatus: number;
  createDate: Date;
  createDateDisplay: string = "";
  countries: string = "";
  countryCount: number = 0;
  isCA: boolean = false;
  isCB: boolean = false;
  cultureAuditLink: string = "";
}

export class CertAppCountryInfo {
  name: string;
  applyingForCertification: boolean;
}

export class FindCurrentEngagementInfoResult {
  errorOccurred: boolean;
  errorMessage: string;
  errorId: number;
  foundActiveEngagement: boolean;
  engagementId: number;
  certificationStatus: string;
  misc: string;
  clientId: number;
  clientName: string;
}

export class FindMostRecentCertificationEIDResult {
  errorOccurred: boolean;
  errorMessage: string;
  foundCertificationEngagement: boolean;
  engagementId: string;
  certDate: string;
  certExpiryDate: string;
  certCountryCode: string;
  affiliateId: string;
}

export class GetEngagementInfoResult {
  errorOccurred: boolean;
  errorMessage: string;
  foundActiveEngagement: boolean;
  eInfo: EngagementInfo;
  curCertInfo: CurrentCertificationInfo;
}

export class ListDeadlineInfo {
  listDeadlineDate: string;
  listName: string;
}

export class GetListDeadlineInfoResult {
  errorOccurred: boolean;
  errorMessage: string;
  listDeadlineInfo: ListDeadlineInfo;
}

export class GetHelpDeskLoginURLResult {
  errorOccurred: boolean;
  errorMessage: string;
  url: string;
}

export class CurrentCertificationInfo {
  engagementId: number;
  continueToShowEmpAndReports: boolean;
  currentlyCertified: boolean;
  certificationStartDate: string;
  certificationExpiryDate: string;
  empResultsDate: string;
  reportDownloadsDate: string;
  trustIndexSSOLink: string;
  profilePublishedLink: string;
}

export class EngagementInfo {
  clientId: string;
  engagementId: number;
  certificationStatus: string;
  clientName: string;
  tier: string;
  journeyStatus: string;
  trustIndexSSOLink: string;
  trustIndexStatus: string;
  trustIndexSourceSystemSurveyId: string;
  cultureAuditSSOLink: string;
  cultureAuditStatus: string;
  cultureAuditSourceSystemSurveyId: string;
  cultureBriefSSOLink: string;
  cultureBriefStatus: string;
  cultureBriefSourceSystemSurveyId: string;
  companyName: string;
  reportDelivery: string;
  reviewCenterPublishedLink: string;
  bestCompDeadline: string;
  isLatestECR: boolean;
  isAbandoned: boolean;
  affiliateId: string;
  isMNCCID: boolean;
  ecrCreationDate: string;
  ecrCountries: string;
  countriesCount: number;
}

export class GetCompanyNameResult {
  errorOccurred: boolean;
  errorMessage: string;
  companyName: string;
}

export class GeneralCallResult {
  success: boolean;
  errorMessage: string;
}

export class ContactInfo {
  firstName: string;
  lastName: string;
  emailAddress: string;
}

export class GenericResult {
  isError: boolean;
  errorStr: string;
}

export class AddUpdateContactResult {
  isError: boolean;
  errorId: number;
  errorStr: string;
}

export class SetPortalContactLoginDateResult {
  isError: boolean;
  errorStr: string;
}

export class GetCompanyUsersResult {
  isError: boolean;
  errorStr: string;
  portalContacts: PortalContact[];
}

export class PortalContact {
  firstName: string;
  lastName: string;
  email: string;
  achievementNotification: boolean;
  hasTenantIdClaim: boolean;
}

export class NewECR {
  affiliateId: string;
  clientId: number;
  companyName: string;
  totalUSEmployees: number;
  productType: string;
  salesforceOpportunityId: string;
  primaryContact: ContactInfo;
}

export class ProductAnswerOption {
  name: string;
  value: string;
}

export class ClientConfiguration {
  AuthServerAuthority: string;
  ApplicationInsightsKey: string;
  GPTWWebSiteBaseUrl: string;
  GPTWStoreUrl: string;
  ZendeskAPIUrl: string;
  EmprisingUrl: string;
  IsAppMaintenanceMode: string;
  ExpectedEnvironmentClaim: string;
}

export class GetClientConfigurationResult {
  errorOccurred: boolean;
  errorMessage: string;
  configuration: ClientConfiguration;
}

export class CreateECRRequest {
  affiliateId: string = "";
  username: string = "";
  password: string = "";
  clientId: number;
  clientName: string = "";
  trustIndexSurveyType: string = "";
  countryCode: string;
  totalEmployees: number;
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  sessionId: string = "";
  callerEmail: string = "";
  achievementNotification: boolean = false;
}

export class CreateECRResult {
  isError: boolean;
  errorStr: string;
  errorId: number;
  warningStr: string;
  engagementId: number;
}

export class GetClientEmailDataResult {
  isError: boolean;
  errorStr: string;
  data: ClientEmail[];
}

export class ClientEmail {
  id: string;
  emailType: string;
  clientId: number;
  engagementId: number;
  dateTimeSent: Date;
  subject: string;
  body: string;
  address: string;
  opened: boolean;
  dateTimeOpened: Date;
  dateTimeOpenedList: Date[];
  isError: boolean;
  errorMessage: string;
}

export class GetDataRequestDataResult {
  isError: boolean;
  errorStr: string;
  data: ClientEmail[];
}

export class DataRequest {
  id: string;
  emailType: string;
  clientId: number;
  engagementId: number;
  dateTimeSent: Date;
  subject: string;
  body: string;
  address: string;
  opened: boolean;
  dateTimeOpened: Date;
  dateTimeOpenedList: Date[];
  isError: boolean;
  errorMessage: string;
}

export interface Claim {
  claimType: string;
  claimValue: string;
}

export class GetClientRecognitionInfoRequest {
  token: string = "";
  clientId: number;
}

export class ListCalendarResult {
  isError: boolean;
  errorStr: string;
  listCalendar: ListCalendar[];
}
export class ListCalendar {
  name: string;
  certified_by: string;
  publish_up: string;
  url: string;
  italicize: boolean;
}


export interface GetClientRecognitionInfoResult {
  isError: boolean;
  errorStr: string;
  clientRecognitionInfoDetails: ClientRecognitionInfoDetail[];
}

export interface ClientRecognitionInfoDetail {
  yearly_list_id: number;
  list_name: string;
  list_year: number;
  rank: number;
  publication_date: Date;
  dashboard_date: Date;
  list_logo_link: string;
  toolkit_is_static: boolean;
  toolkit_custom_content: string;
  list_url: string;
}

export interface GetCertificationToolkitResult {
  isError: boolean;
  errorStr: string;
  toolkitContent: string;
}

export interface EmailToolkitResult {
  isError: boolean;
  erorStr: string;
}

export interface FavoriteClickedResult {
  isError: boolean;
  erorStr: string;
}

export class SetStatusResult {
  isError: boolean;
  errorStr: string;
}

export class OptOutAbandonSurveyResult {
  isError: boolean;
  errorStr: string;
}

export class ClientPhoto {
  fileName: string = "";
  caption: string = "";
  primary: boolean = false;
}

export class SaveClientImagesRequest {
  cultureSurveyId: string;
  clientPhotos: ClientPhoto[];
  logoFileName: string;
}

export class DeleteClientImageRequest {
  cultureSurveyId: string;
  fileName: string;
}

export class DeleteClientImageResult {
  errorOccurred: boolean;
  errorMessage: string;
}

export class ErrorHighlights {
  public errorText: string;
  public highlightVars: string[];
  public hoverText: string;
}

export class GetClientImageInfoResult {
  errorOccurred: boolean;
  errorMessage: string;
  surveyId: string;
  surveyStatus: string;
  clientPhotos: ClientPhoto[];
  logoFileName: string;
  engagementId: number;
}

export class RepublishProfileResult {
  isError: boolean;
  errorStr: string;
  reviewPublishStatus: string;
}

export class RepublishProfileRequest {
  username: string = "";
  password: string = "";
  clientId: number;
  engagementId: number;
}

export class SetCustomerActivationStatusResult {
  isError: boolean;
  errorStr: string;
}

export class CreateHelpDeskTicketResult {
  isError: boolean;
}

export class HeaderNotifyEvent {
  companyName: string;
  clientId: string;
}

export class HeaderInfo {
  clientId = -1;
  engagementId = -1;
  clientName: string = "";
}

export class GetDashboardDataResult {
  isError: boolean;
  errorStr: string;
  ecrV2s: ECRV2Info[];
}
export class ReturnAffiliates {
  isSuccess: boolean;
  errorMessage: string;
  affiliates: AffiliateInfo[];
}
export class AffiliateInfo {
  id: string;
  affiliateId: string;
  affiliateName: string;
  startClientId: number;
  endClientId: number;
  defaultCountryCode: string;
}

export class ReturnCountries {
  isSuccess: boolean;
  errorMessage: string;
  countries: CountryInfo[];
}

export class ReturnCountry {
  isSuccess: boolean;
  errorMessage: string;
  country: CountryInfo;
}

export class CountryInfo {
  id: string;
  countryCode: string;
  countryName: string;
  badgeLanguage: string;
  shortNameForBadge: string;
  currencySymbol: string;
  currencyLocalId: string;
  facebookLink: string;
  instagramLink: string;
  linkedInLink: string;
  twitterLink: string;
}

export class LanguageInfo {
  id: string;
  cultureId: string;
  language: string;
  countryName: string;
}

export class PostUserEventResult {
  isError: boolean;
  errorStr: string;
  userEventEnumName: UserEventName;
}

export class AppConfigDetailsResult {
  isError: boolean;
  errorStr: string;
  isAppMaintenanceMode: "";
  appVersion: "";
}

export class EcrSearchResult {
  isError: boolean;
  errorStr: string;
  affiliates: AffiliateInfo[];
  numCNameMatches: number;
  numCIDMatches: number;
  numEIDMatches: number;
}

export enum UserEventName {
  toolkitPageView = 0,
  toolkitDownloadBadge,
  toolkitEmailFriend,
  toolkitViewUsageGuidlines,
  toolkitViewPressRelease,
  toolkitVisitStore,
  toolkitDownloadShareableImage1,
  toolkitDownloadShareableImage2,
  toolkitDownloadShareableImage3,
  toolkitDownloadShareableImage4,
  toolkitDownloadBadgeSVG,
  toolkitDownloadBadgeJPG,
  toolkitDownloadBadgePNG,
  toolkitDownloadBadgeZIP,
  toolkitCelebrationKit,
  toolkitShareToolkit,
  engagementGoToTI,
  engagementGotoCB,
  engagementGoToCA,
  engagementReportDownload
}

export enum userEventSource {
  portal = 0,
  cultureSurvey
}

export enum userEventUserType {
  employee = 0,
  endUser
}

export class ECRV2Info {
  id: string;
  cid: number;
  eid: number;
  createdate;
  cname: string;
  tistatus: string;
  cbstatus: string;
  castatus: string;
  cstatus: string;
  tools: string;
  tilink: string;
  cblink: string;
  calink: string;
  rstatus: string;
  rlink: string;
  lstatus: string;
  country: string;
  certexdate;
  tier: string;
  journeystatus: string;
  journeyhealth: string;
  duration: number;
  renewalstatus: string;
  renewalhealth: string;
  engagementstatus: string;
  engagementhealth: string;
  numberofsurveyrespondents: number;
  abandoned: boolean;
  favorite: boolean;
  surveyopendate;
  surveyclosedate;
  allcountrycertification: string;
  allcountrylisteligiblity: string;
}

export class SurveyCreateMessage {
  affiliateId: string;
  clientId: number;
  clientName: string;
  cultureInitiativeName: string;
  surveyName: string;
  createMode: string;
  createDetails: CreateDetails;
}

export class CreateDetails {
  engagementId: number;
  productTier: string;
  countryList: CountryList[];
}
export class CountryList {
  countryCode: string;
  numberOfEmployeesInCountry: number;
}

export interface ClientEmailsGridSettings {
  version: number;
  showMyFavorites: boolean;
  selectedAffiliateId: string;
  columnsConfig: ColumnSettings[];
  state: State;
  pageSize: number;
  pageSizes: PageSizeItem[];
  gridData?: DataResult;
}

export interface DataRequestGridSettings {
  version: number;
  showMyFavorites: boolean;
  selectedAffiliateId: string;
  columnsConfig: ColumnSettings[];
  state: State;
  pageSize: number;
  pageSizes: PageSizeItem[];
  gridData?: DataResult;
}

export interface GridSettings {
  version: number;
  showMyFavorites: boolean;
  selectedAffiliateId: string;
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


export const DefaultDataRequestGridSettings: DataRequestGridSettings = {
  version: 0.9,
  showMyFavorites: false,
  selectedAffiliateId: "",
  state: {
    skip: 0,
    take: 100,
    // Initial filter descriptor
    filter: {
      logic: 'and',
      filters: []
    },
    sort: [{
      dir: 'desc',
      field: 'dateTimeSent'
    }]
  },
  pageSize: 100,
  pageSizes: [{ text: '100', value: 100 }, { text: '500', value: 500 }, { text: '1000', value: 1000 }, { text: 'All', value: 'all' }],
  gridData: null,
  columnsConfig: [{
    field: 'id',
    title: 'Id',
    hidden: true,
    filterable: true,
    _width: 90
  },
  {
    field: 'dateRequested',
    title: 'Requested Date',
    hidden: false,
    filter: 'date',
    format: '{0:d}',
    _width: 45,
    filterable: true
  },
  {
    field: 'status',
    title: 'Status',
    hidden: false,
    filterable: true,
    _width: 45
  },
  {
    field: 'downloadLink',
    title: 'Download',
    hidden: false,
    filterable: true,
    _width: 100
  }]
};


export const DefaultClientEmailGridSettings: ClientEmailsGridSettings = {
  version: 0.9,
  showMyFavorites: false,
  selectedAffiliateId: "",
  state: {
    skip: 0,
    take: 100,
    // Initial filter descriptor
    filter: {
      logic: 'and',
      filters: []
    },
    sort: [{
      dir: 'desc',
      field: 'dateTimeSent'
    }]
  },
  pageSize: 100,
  pageSizes: [{ text: '100', value: 100 }, { text: '500', value: 500 }, { text: '1000', value: 1000 }, { text: 'All', value: 'all' }],
  gridData: null,
  columnsConfig: [{
    field: 'id',
    title: 'Id',
    hidden: true,
    filterable: true,
    _width: 90
  },
  {
      field: 'clientId',
      title: 'CID',
      hidden: true,
      filterable: true,
      _width: 45
  },
  {
      field: 'engagementId',
      title: 'EID',
      hidden: true,
      filterable: true,
      _width: 45
  },
  {
      field: 'dateTimeSent',
      title: 'Sent',
      hidden: false,
      filter: 'date',
      format: '{0:d}',
      _width: 45,
      filterable: true
  },
  {
      field: 'opened',
      title: 'Opened',
      hidden: false,
      filterable: true,
      _width: 45
  },
  {
    field: 'dateTimeOpened',
    title: 'OpenDate',
    hidden: true,
    filter: 'date',
    format: '{0:d}',
    _width: 45,
    filterable: true
  },
  {
      field: 'address',
      title: 'Address',
      hidden: false,
      filterable: true,
      _width: 160
  },
  {
      field: 'emailType',
      title: 'Type',
      hidden: false,
      filterable: true,
      _width: 58
  },
  {
      field: 'subject',
      title: 'Subject',
      hidden: false,
      filterable: true,
      _width: 140
  },
  {
      field: 'body',
      title: 'View',
      hidden: false,
      filterable: true,
      _width: 35
  },
  {
      field: 'isError',
      title: 'Error',
      hidden: false,
      filterable: true,
      _width: 35
  },
  {
      field: 'errorMessage',
      title: 'ErrorInfo',
      hidden: true,
      filterable: true,
      _width: 100
  }]
};


export const DefaultGridSettings: GridSettings = {
  version: 1.10,
  showMyFavorites: false,
  selectedAffiliateId: "",
  state: {
    skip: 0,
    take: 100,
    // Initial filter descriptor
    filter: {
      logic: 'and',
      filters: []
    },
    sort: [{
      dir: 'desc',
      field: 'createdate'
    }]
  },
  pageSize: 100,
  pageSizes: [{ text: '100', value: 100 }, { text: '500', value: 500 }, { text: '1000', value: 1000 }, { text: 'All', value: 'all' }],
  gridData: null,
  columnsConfig: [{
    field: 'favorite',
    title: 'Fav',
    hidden: false,
    filterable: false,
    _width: 58
  }, {
    field: 'tier',
    title: 'Tier',
    hidden: false,
    _width: 58,
    filterable: true
  }, {
    field: 'cname',
    title: 'Company Name',
    hidden: false,
    filterable: true,
    _width: 200
  }, {
    field: 'cid',
    title: 'ClientId',
    hidden: true,
    filter: 'numeric',
    format: '{nO}',
    _width: 100,
    filterable: true
  }, {
    field: 'eid',
    title: 'EngagementId',
    hidden: true,
    filter: 'numeric',
    format: '{nO}',
    _width: 100,
    filterable: true
  }, {
    field: 'journeystatus',
    title: 'Status (Journey)',
    hidden: false,
    _width: 100,
    filterable: true
  }, {
    field: 'journeyhealth',
    title: 'Health (Journey)',
    hidden: false,
    _width: 100,
    filterable: true
  }, {
    field: 'duration',
    title: 'Duration (Journey)',
    hidden: false,
    filter: 'numeric',
    format: '{nO}',
    _width: 100,
    filterable: true
  }, {
    field: 'tistatus',
    title: 'TI',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'cbstatus',
    title: 'CB',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'cstatus',
    title: 'Cert.',
    hidden: false,
    _width: 95,
    filterable: true
  }, {
    field: 'certexdate',
    title: 'CertEx',
    hidden: true,
    filter: 'date',
    format: '{0:d}',
    _width: 70,
    filterable: true
  }, {
    field: 'castatus',
    title: 'CA',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'lstatus',
    title: 'List',
    hidden: false,
    _width: 95,
    filterable: true
  }, {
    field: 'country',
    title: 'Country',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'rstatus',
    title: 'Profile',
    hidden: false,
    _width: 95,
    filterable: true
  }, {
    field: 'createdate',
    title: 'Created',
    hidden: false,
    filter: 'date',
    format: '{0:d}',
    _width: 100,
    filterable: true
  }, {
    field: 'tools',
    title: 'Tools',
    hidden: false,
    _width: 70,
    filterable: true
  }, {
    field: 'surveyopendate',
    title: 'Survey Open',
    hidden: true,
    filter: 'date',
    format: '{0:d}',
    _width: 100,
    filterable: true
  }, {
    field: 'surveyclosedate',
    title: 'Survey Close',
    hidden: true,
    filter: 'date',
    format: '{0:d}',
    _width: 100,
    filterable: true
  }, {
    field: 'engagementstatus',
    title: 'Status (Engagement)',
    hidden: true,
    _width: 100,
    filterable: true
  }, {
    field: 'engagementhealth',
    title: 'Health (Engagement)',
    hidden: true,
    _width: 100,
    filterable: true
  }, {
    field: 'renewalstatus',
    title: 'Status (Renewal)',
    hidden: true,
    _width: 100,
    filterable: true
  }, {
    field: 'renewalhealth',
    title: 'Health (Renewal)',
    hidden: true,
    _width: 100,
    filterable: true
  }, {
    field: 'numberofsurveyrespondents',
    title: '# Respondents',
    hidden: true,
    filter: 'numeric',
    format: '{nO}',
    _width: 100,
    filterable: true
  }]
};

export class GetCountriesForDataRequestResult {
  isError: boolean;
  errorStr: string;
  countries: CountryInfo[];
}

export class DataRequestInfo {
  requestorEmail: string;
  affiliateId: string;
  uploadedFileName: string;
}

export class DataRequestInfoResult {
  isError: boolean;
  errorStr: string;
}

export class GetDataExtractRequestsResult {
  isError: boolean;
  errorStr: string;
  dataExtractRequests: DataExtractRequest[];
}

export class DataExtractRequest {
  id: string;
  dateRequested;
  status: string;
  link: string;
  affiliateId: string;
}

export class CreateEcrRequest2 {
  affiliateId: string = "";
  username: string = "";
  password: string = "";
  clientId: number;
  clientName: string = "";
  trustIndexSurveyType: string = "";
  countryCode: string;
  totalEmployees: number;
  email: string = "";
  firstName: string = "";
  lastName: string = "";
  sessionId: string = "";
  callerEmail: string = "";
  achievementNotification: boolean = false;
  isCreateCA: boolean = false;
  surveyCountries: SurveyCountry[] = [];
}

export class SurveyCountry {
  countryCode: string = "";
  totalEmployees: number;
  isApplyForCertification: boolean = false;
}

export class GetDataExtractCompanyInfoByClientIdResult {
  isSuccess: boolean = false;
  errorMessage: string = "";
  dataExtractCompanyInfos: DataExtractCompanyInfo[] = [];
}

export class DataExtractCompanyInfo {
  clientId: number;
  engagementId: number;
  clientName: string = "";
}

export class DataExtractRequestData {
  requestorEmail: string = "";
  fileName: string = "";
  countryCode: string = "";
  trustIndexData: string = "";
  cultureBriefDatapoints: string = "";
  cultureAuditEssays: string = "";
  photosAndCaptions: string = "";
  certificationExpiry: string = "";
  completedCultureAudit: string = "";
  industry: string = "";
  industryVertical: string = "";
  industry2: string = "";
  industryVertical2: string = "";
  minimumNumberEmployees: string = "";
  maximumNumberEmployees: string = "";
  dataExtractCompanyInfos: DataExtractCompanyInfo[] = [];
}

export class GetDataExtractRequestResult extends DataExtractRequestData {
  isSuccess: boolean = false;
  errorMessage: string = "";
  status: string = "";
  reportLink: string = "";
  howToSelectCompanies: string = "";
}
