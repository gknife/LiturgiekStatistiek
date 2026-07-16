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
  denomination: string | null;
  status: string | null;
  statusValue: number;
  createdBy?: string | null;
  createdAt?: string | null;
  modifiedBy?: string | null;
  modifiedAt?: string | null;
}

export interface ServiceDetail {
  id: string;
  date: string;
  timeOfDay: string;
  congregation: CongregationSummary;
  preacher: PreacherSummary | null;
  churchCalendarSunday: string | null;
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
  musicalAccompanimentId: string | null;
  specialOccasionId: string | null;
  sermonTextReferences: SermonTextReference[];
  denomination: string | null;
  denominationId: string | null;
  status: string | null;
  statusValue: number;
}

export interface ServiceElement {
  id: string;
  position: number;
  elementType: string;
  label: string | null;
  labelId: string | null;
  scriptureReference: string | null;
  notes: string | null;
  songs: ServiceElementSong[];
  performerId: string | null;
  performer: string | null;
  isBeurtzang: boolean;
  bibleTranslationId: string | null;
  bibleTranslation: string | null;
  readingReferences: ReadingReference[] | null;
}

export interface ReadingReference {
  bibleBookId: string | null;
  bookName: string;
  chapter: number | null;
  verseStart: number | null;
  verseEnd: number | null;
  position: number;
}

export interface SongCompleteness {
  state: string;
  completeInElement: boolean;
  completeInService: boolean;
  catalogVerseCount: number | null;
  sungVerseCount: number;
}

export interface ServiceElementSong {
  id: string;
  bundleName: string;
  bundleAbbreviation: string | null;
  section: string;
  songNumber: number;
  position: number;
  verses: string[];
  bundleId: string | null;
  completeness: SongCompleteness | null;
  sungInFull?: boolean;
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
  denominationId: string | null;
  serviceCount: number;
  pastors: CongregationPastor[] | null;
}

export interface CongregationPastor {
  preacherId: string;
  fullName: string;
  city: string | null;
  isPrimary: boolean;
}

export interface CongregationPastorInput {
  preacherId: string;
  isPrimary: boolean;
}

export interface CongregationSummary {
  id: string;
  name: string;
  city: string;
  denominationAbbreviation: string | null;
  denominationId: string | null;
  pastors: CongregationPastor[] | null;
}

export interface Preacher {
  id: string;
  fullName: string;
  denomination: string | null;
  city: string | null;
  denominationId: string | null;
  titleId: string | null;
  title: string | null;
  serviceCount: number;
}

export interface PreacherSummary {
  id: string;
  fullName: string;
  city: string | null;
  title: string | null;
  denomination: string | null;
}

export interface CreateCongregationRequest {
  name: string;
  city: string;
  locationDetail: string | null;
  denominationId: string | null;
  modality: string | null;
  latitude: number | null;
  longitude: number | null;
}

export interface UpdateCongregationRequest extends CreateCongregationRequest {
  pastors: CongregationPastorInput[] | null;
}

export interface CreatePreacherRequest {
  fullName: string;
  denominationId: string | null;
  city: string | null;
  titleId: string | null;
}

export type UpdatePreacherRequest = CreatePreacherRequest;

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
  liturgicalElementType?: number | null;
  createdBy?: string | null;
  createdAt?: string | null;
  modifiedBy?: string | null;
  modifiedAt?: string | null;
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

// ---------------- Service templates (Sjablonen) ----------------

export interface ServiceTemplateElement {
  id: string;
  position: number;
  elementType: string;
  elementTypeValue: number;
  labelId: string | null;
  label: string | null;
  performerId: string | null;
  performer: string | null;
  isBeurtzang: boolean;
  fixedScriptureReference: string | null;
}

export interface ServiceTemplate {
  id: string;
  name: string;
  denominationId: string | null;
  denomination: string | null;
  congregationId: string | null;
  congregation: string | null;
  timeOfDay: number | null;
  occasionId: string | null;
  occasion: string | null;
  isActive: boolean;
  elements: ServiceTemplateElement[];
  musicalAccompanimentId?: string | null;
  musicalAccompaniment?: string | null;
  isReadingService?: boolean;
  hasBeamerLiturgy?: boolean;
  hasBeamerTexts?: boolean;
  hasBeamerSongs?: boolean;
  defaultBibleTranslationId?: string | null;
  defaultBibleTranslation?: string | null;
  defaultSongBundleId?: string | null;
  defaultSongBundle?: string | null;
}

export interface ServiceTemplateSummary {
  id: string;
  name: string;
  denominationId: string | null;
  denomination: string | null;
  congregationId: string | null;
  congregation: string | null;
  timeOfDay: number | null;
  occasionId: string | null;
  occasion: string | null;
  isActive: boolean;
  elementCount: number;
}

export interface CreateServiceTemplateElementRequest {
  position: number;
  elementType: number;
  labelId: string | null;
  performerId: string | null;
  isBeurtzang: boolean;
  fixedScriptureReference: string | null;
}

export interface CreateServiceTemplateRequest {
  name: string;
  denominationId: string | null;
  congregationId: string | null;
  timeOfDay: number | null;
  occasionId: string | null;
  isActive: boolean;
  elements: CreateServiceTemplateElementRequest[];
  musicalAccompanimentId?: string | null;
  isReadingService?: boolean;
  hasBeamerLiturgy?: boolean;
  hasBeamerTexts?: boolean;
  hasBeamerSongs?: boolean;
  defaultBibleTranslationId?: string | null;
  defaultSongBundleId?: string | null;
}

/** A ready-to-use service element produced by instantiating a template. */
export interface TemplateElementInstance {
  position: number;
  elementType: number;
  labelId: string | null;
  scriptureReference: string | null;
  notes: string | null;
  songs: unknown[] | null;
  performerId: string | null;
  isBeurtzang: boolean;
  bibleTranslationId: string | null;
  readingReferences: ReadingReference[] | null;
}
