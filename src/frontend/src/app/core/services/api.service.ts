import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PaginatedResult,
  ServiceSummary,
  ServiceDetail,
  Congregation,
  CongregationSummary,
  Preacher,
  PreacherSummary,
  ListDefinition,
  Song,
  SongVerse,
  ContentPage,
  ListItem,
  AiStatus,
  AdvancedQuerySchema,
  AdvancedQueryDefinition,
  SavedQuery,
  BibleBook,
  LiturgyParseResult,
  ServiceTemplate,
  ServiceTemplateSummary,
  CreateServiceTemplateRequest,
  TemplateElementInstance,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Services
  getServices(params?: {
    page?: number;
    pageSize?: number;
    congregationId?: string;
    fromDate?: string;
    toDate?: string;
    denominationId?: string;
    includeConcepts?: boolean;
  }): Observable<PaginatedResult<ServiceSummary>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.congregationId) httpParams = httpParams.set('congregationId', params.congregationId);
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);
    if (params?.denominationId) httpParams = httpParams.set('denominationId', params.denominationId);
    if (params?.includeConcepts !== undefined) httpParams = httpParams.set('includeConcepts', params.includeConcepts);
    return this.http.get<PaginatedResult<ServiceSummary>>(`${this.baseUrl}/services`, { params: httpParams });
  }

  getService(id: string): Observable<ServiceDetail> {
    return this.http.get<ServiceDetail>(`${this.baseUrl}/services/${id}`);
  }

  createService(request: any): Observable<ServiceDetail> {
    return this.http.post<ServiceDetail>(`${this.baseUrl}/services`, request);
  }

  updateService(id: string, request: any): Observable<ServiceDetail> {
    return this.http.put<ServiceDetail>(`${this.baseUrl}/services/${id}`, request);
  }

  publishService(id: string): Observable<ServiceDetail> {
    return this.http.post<ServiceDetail>(`${this.baseUrl}/services/${id}/publish`, {});
  }

  deleteService(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/services/${id}`);
  }

  // Congregations
  getCongregations(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
  }): Observable<PaginatedResult<Congregation>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.search) httpParams = httpParams.set('search', params.search);
    return this.http.get<PaginatedResult<Congregation>>(`${this.baseUrl}/congregations`, { params: httpParams });
  }

  searchCongregations(query: string): Observable<CongregationSummary[]> {
    return this.http.get<CongregationSummary[]>(`${this.baseUrl}/congregations/search`, {
      params: new HttpParams().set('q', query),
    });
  }

  createCongregation(request: any): Observable<Congregation> {
    return this.http.post<Congregation>(`${this.baseUrl}/congregations`, request);
  }

  // Preachers
  getPreachers(params?: {
    page?: number;
    pageSize?: number;
    search?: string;
  }): Observable<PaginatedResult<Preacher>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.search) httpParams = httpParams.set('search', params.search);
    return this.http.get<PaginatedResult<Preacher>>(`${this.baseUrl}/preachers`, { params: httpParams });
  }

  searchPreachers(query: string): Observable<PreacherSummary[]> {
    return this.http.get<PreacherSummary[]>(`${this.baseUrl}/preachers/search`, {
      params: new HttpParams().set('q', query),
    });
  }

  createPreacher(request: any): Observable<Preacher> {
    return this.http.post<Preacher>(`${this.baseUrl}/preachers`, request);
  }

  // Lists
  getAllLists(): Observable<ListDefinition[]> {
    return this.http.get<ListDefinition[]>(`${this.baseUrl}/lists`);
  }

  getListByName(name: string): Observable<ListDefinition> {
    return this.http.get<ListDefinition>(`${this.baseUrl}/lists/${name}`);
  }

  addListItem(request: { listDefinitionId: string; value: string; abbreviation?: string | null; sortOrder: number }): Observable<ListItem> {
    return this.http.post<ListItem>(`${this.baseUrl}/lists/items`, request);
  }

  updateListItem(id: string, request: { value: string; abbreviation?: string | null; sortOrder: number; isActive: boolean; liturgicalElementType?: number | null }): Observable<ListItem> {
    return this.http.put<ListItem>(`${this.baseUrl}/lists/items/${id}`, request);
  }

  deleteListItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/lists/items/${id}`);
  }

  // Songs
  getSongsByBundle(bundleId: string, page = 1, pageSize = 50): Observable<PaginatedResult<Song>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PaginatedResult<Song>>(`${this.baseUrl}/songs/bundle/${bundleId}`, { params });
  }

  getSongByNumber(bundleId: string, number: number): Observable<Song> {
    return this.http.get<Song>(`${this.baseUrl}/songs/bundle/${bundleId}/number/${number}`);
  }

  getSong(id: string): Observable<Song> {
    return this.http.get<Song>(`${this.baseUrl}/songs/${id}`);
  }

  createSong(request: { bundleId: string; section?: string; number: number; title?: string; numberOfVerses?: number; verses?: SongVerse[] }): Observable<Song> {
    return this.http.post<Song>(`${this.baseUrl}/songs`, request);
  }

  updateSong(id: string, request: { section?: string; number?: number; title?: string; numberOfVerses?: number; verses?: SongVerse[] }): Observable<Song> {
    return this.http.put<Song>(`${this.baseUrl}/songs/${id}`, request);
  }

  deleteSong(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/songs/${id}`);
  }

  // Content
  getContent(slug: string): Observable<ContentPage> {
    return this.http.get<ContentPage>(`${this.baseUrl}/content/${slug}`);
  }

  updateContent(slug: string, request: { titleNl: string; contentMarkdown: string }): Observable<ContentPage> {
    return this.http.put<ContentPage>(`${this.baseUrl}/content/${slug}`, request);
  }

  // Queries
  getQueryTemplates(): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/queries/templates`);
  }

  executeQuery(request: { templateId?: string; naturalLanguageQuery?: string; parameters?: Record<string, string> }): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/queries/execute`, request);
  }

  getAiStatus(): Observable<AiStatus> {
    return this.http.get<AiStatus>(`${this.baseUrl}/queries/ai-status`);
  }

  // Advanced query builder
  getAdvancedSchema(): Observable<AdvancedQuerySchema> {
    return this.http.get<AdvancedQuerySchema>(`${this.baseUrl}/queries/advanced/schema`);
  }

  executeAdvancedQuery(definition: AdvancedQueryDefinition): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/queries/advanced/execute`, definition);
  }

  compareAdvancedQueries(queries: AdvancedQueryDefinition[]): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/queries/advanced/compare`, { queries });
  }

  exportAdvancedExcel(definition: AdvancedQueryDefinition): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export/advanced-excel`, definition, { responseType: 'blob' });
  }

  // Saved queries (authenticated)
  getSavedQueries(): Observable<SavedQuery[]> {
    return this.http.get<SavedQuery[]>(`${this.baseUrl}/queries/saved`);
  }

  createSavedQuery(request: { name: string; queryParameters: string; isPublic: boolean }): Observable<SavedQuery> {
    return this.http.post<SavedQuery>(`${this.baseUrl}/queries/saved`, request);
  }

  deleteSavedQuery(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/queries/saved/${id}`);
  }

  // Bulk service operations
  bulkUpdateServices(request: { serviceIds: string[]; field: string; value: string | null }): Observable<{ affected: number }> {
    return this.http.post<{ affected: number }>(`${this.baseUrl}/services/bulk-update`, request);
  }

  bulkDeleteServices(request: { serviceIds: string[] }): Observable<{ affected: number }> {
    return this.http.post<{ affected: number }>(`${this.baseUrl}/services/bulk-delete`, request);
  }

  // Export
  exportExcel(request: any): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export/excel`, request, { responseType: 'blob' });
  }

  // Bible books (for the structured Preektekst editor)
  getBibleBooks(translation?: string): Observable<BibleBook[]> {
    let params = new HttpParams();
    if (translation) params = params.set('translation', translation);
    return this.http.get<BibleBook[]>(`${this.baseUrl}/bible/books`, { params });
  }

  // Liturgy parsing (paste + URL import)
  parseLiturgy(text: string, title?: string): Observable<LiturgyParseResult> {
    return this.http.post<LiturgyParseResult>(`${this.baseUrl}/parse/liturgy`, { text, title });
  }

  importLiturgyUrl(url: string): Observable<LiturgyParseResult> {
    return this.http.post<LiturgyParseResult>(`${this.baseUrl}/parse/url`, { url });
  }

  // Service templates (Sjablonen)
  getTemplates(): Observable<ServiceTemplateSummary[]> {
    return this.http.get<ServiceTemplateSummary[]>(`${this.baseUrl}/templates`);
  }

  getTemplate(id: string): Observable<ServiceTemplate> {
    return this.http.get<ServiceTemplate>(`${this.baseUrl}/templates/${id}`);
  }

  createTemplate(request: CreateServiceTemplateRequest): Observable<ServiceTemplate> {
    return this.http.post<ServiceTemplate>(`${this.baseUrl}/templates`, request);
  }

  updateTemplate(id: string, request: CreateServiceTemplateRequest): Observable<ServiceTemplate> {
    return this.http.put<ServiceTemplate>(`${this.baseUrl}/templates/${id}`, request);
  }

  deleteTemplate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/templates/${id}`);
  }

  instantiateTemplate(params: {
    denominationId?: string | null;
    congregationId?: string | null;
    timeOfDay?: number | null;
    occasionId?: string | null;
  }): Observable<TemplateElementInstance[]> {
    let httpParams = new HttpParams();
    if (params.denominationId) httpParams = httpParams.set('denominationId', params.denominationId);
    if (params.congregationId) httpParams = httpParams.set('congregationId', params.congregationId);
    if (params.timeOfDay !== null && params.timeOfDay !== undefined) httpParams = httpParams.set('timeOfDay', params.timeOfDay);
    if (params.occasionId) httpParams = httpParams.set('occasionId', params.occasionId);
    return this.http.get<TemplateElementInstance[]>(`${this.baseUrl}/templates/instantiate`, { params: httpParams });
  }

  // User settings (authenticated)
  getUserSettings(): Observable<{ settingsJson: string }> {
    return this.http.get<{ settingsJson: string }>(`${this.baseUrl}/usersettings`);
  }

  saveUserSettings(settingsJson: string): Observable<{ settingsJson: string }> {
    return this.http.put<{ settingsJson: string }>(`${this.baseUrl}/usersettings`, { settingsJson });
  }

  // Recent searches (authenticated)
  getRecentSearches(max = 10): Observable<{ queryText: string; createdAt: string }[]> {
    return this.http.get<{ queryText: string; createdAt: string }[]>(`${this.baseUrl}/recent-searches`, {
      params: new HttpParams().set('max', max),
    });
  }

  addRecentSearch(queryText: string): Observable<{ queryText: string; createdAt: string }> {
    return this.http.post<{ queryText: string; createdAt: string }>(`${this.baseUrl}/recent-searches`, { queryText });
  }

  clearRecentSearches(): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/recent-searches`);
  }
}
