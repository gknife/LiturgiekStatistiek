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
  broadcastUrl: string | null;
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
  timeOfDayValue: number;
  churchCalendarSundayId: string | null;
  bibleTranslationId: string | null;
  musicalAccompanimentId: string | null;
  specialOccasionId: string | null;
  sermonTextReferences: SermonTextReference[];
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
  section: string;
  songNumber: number;
  position: number;
  verses: string[];
}

export interface SermonTextReference {
  bibleBookId: string | null;
  bookName: string;
  chapter: number | null;
  verseStart: number | null;
  verseEnd: number | null;
  position: number;
}

export interface BibleBook {
  id: string;
  ordinal: number;
  testament: string;
  name: string;
  chapterCount: number;
  verseCounts: number[];
}

export interface ParsedElement {
  position: number;
  label: string;
  songBundle: string | null;
  songNumber: number | null;
  verses: string[] | null;
  notes: string | null;
}

export interface ParsedServiceData {
  city: string | null;
  congregation: string | null;
  denomination: string | null;
  date: string | null;
  timeOfDay: string | null;
  preacher: string | null;
  bibleTranslation: string | null;
  sermonText: string | null;
  sermonTheme: string | null;
  broadcastUrl: string | null;
  elements: ParsedElement[];
}

export interface LiturgyParseResult {
  success: boolean;
  data: ParsedServiceData | null;
  errorMessage: string | null;
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

export interface SongVerse {
  number: number;
  title: string | null;
}

export interface Song {
  id: string;
  bundleId: string;
  bundleName: string;
  bundleAbbreviation: string | null;
  section: string;
  number: number;
  title: string | null;
  numberOfVerses: number | null;
  verses?: SongVerse[] | null;
}

export interface ContentPage {
  id: string;
  slug: string;
  titleNl: string;
  contentMarkdown: string;
  modifiedBy: string | null;
  modifiedAt: string | null;
}

export interface AiStatus {
  isConfigured: boolean;
  hasEndpoint: boolean;
  hasApiKey: boolean;
  deploymentName: string | null;
  message: string;
}

// ---------------- Query result ----------------

export interface QueryResult {
  title: string;
  description: string;
  chartType: string;
  columns: string[];
  rows: Record<string, unknown>[];
  totalCount: number;
  chart: ChartData | null;
}

export interface ChartData {
  labels: string[];
  datasets: ChartDataset[];
}

export interface ChartDataset {
  label: string;
  data: number[];
  backgroundColor: string | null;
}

// ---------------- Advanced query builder ----------------

export interface AdvancedField {
  key: string;
  label: string;
  type: string;
  operators: string[];
  listKey: string | null;
  canGroupBy: boolean;
}

export interface AdvancedQuerySchema {
  fields: AdvancedField[];
  groupByFields: AdvancedField[];
}

export interface AdvancedFilter {
  field: string;
  operator: string;
  value?: string | null;
  value2?: string | null;
  songBundleA?: string | null;
  songNumberA?: number | null;
  songBundleB?: string | null;
  songNumberB?: number | null;
}

export interface AdvancedQueryDefinition {
  name?: string | null;
  filters: AdvancedFilter[];
  outputMode: 'list' | 'aggregate';
  groupBy?: string | null;
  page: number;
  pageSize: number;
  chartType: string;
}

export interface SavedQuery {
  id: string;
  name: string;
  queryParameters: string;
  isPublic: boolean;
  createdAt: string;
}
