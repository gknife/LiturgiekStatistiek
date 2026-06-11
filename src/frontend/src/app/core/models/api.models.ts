export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface ServiceSummary {
  id: string;
  date: string;
  timeOfDay: string;
  congregationName: string;
  city: string | null;
  preacherName: string | null;
  specialOccasion: string | null;
  elementCount: number;
}

export interface ServiceDetail {
  id: string;
  date: string;
  timeOfDay: string;
  congregation: CongregationSummary;
  preacher: PreacherSummary | null;
  churchCalendarSunday: string | null;
  bibleTranslation: string | null;
  isReadingService: boolean;
  readSermonBy: string | null;
  musicalAccompaniment: string | null;
  hasBeamerLiturgy: boolean;
  hasBeamerTexts: boolean;
  hasBeamerSongs: boolean;
  hasBeamerTextsAndSongs: boolean;
  broadcastUrl: string | null;
  specialOccasion: string | null;
  sermonText: string | null;
  sermonTheme: string | null;
  notes: string | null;
  bundles: string[];
  elements: ServiceElement[];
  createdAt: string;
  createdBy: string | null;
}

export interface ServiceElement {
  id: string;
  position: number;
  elementType: string;
  label: string | null;
  scriptureReference: string | null;
  notes: string | null;
  songs: ServiceElementSong[];
}

export interface ServiceElementSong {
  id: string;
  bundleName: string;
  bundleAbbreviation: string | null;
  songNumber: number;
  position: number;
  verses: string[];
}

export interface Congregation {
  id: string;
  name: string;
  city: string;
  locationDetail: string | null;
  denomination: string | null;
  denominationAbbreviation: string | null;
  modality: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface CongregationSummary {
  id: string;
  name: string;
  city: string;
  denominationAbbreviation: string | null;
}

export interface Preacher {
  id: string;
  fullName: string;
  title: string | null;
  denomination: string | null;
  city: string | null;
}

export interface PreacherSummary {
  id: string;
  fullName: string;
  title: string | null;
}

export interface ListDefinition {
  id: string;
  name: string;
  description: string | null;
  isSystemList: boolean;
  items: ListItem[];
}

export interface ListItem {
  id: string;
  value: string;
  abbreviation: string | null;
  sortOrder: number;
  isActive: boolean;
}

export interface Song {
  id: string;
  bundleId: string;
  bundleName: string;
  bundleAbbreviation: string | null;
  number: number;
  title: string | null;
  numberOfVerses: number | null;
}

export interface ContentPage {
  id: string;
  slug: string;
  titleNl: string;
  contentMarkdown: string;
  modifiedBy: string | null;
  modifiedAt: string | null;
}
