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
  ContentPage,
  ListItem,
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
  }): Observable<PaginatedResult<ServiceSummary>> {
    let httpParams = new HttpParams();
    if (params?.page) httpParams = httpParams.set('page', params.page);
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize);
    if (params?.congregationId) httpParams = httpParams.set('congregationId', params.congregationId);
    if (params?.fromDate) httpParams = httpParams.set('fromDate', params.fromDate);
    if (params?.toDate) httpParams = httpParams.set('toDate', params.toDate);
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

  addListItem(request: { listDefinitionId: string; value: string; abbreviation?: string; sortOrder: number }): Observable<ListItem> {
    return this.http.post<ListItem>(`${this.baseUrl}/lists/items`, request);
  }

  updateListItem(id: string, request: { value: string; abbreviation?: string; sortOrder: number; isActive: boolean }): Observable<ListItem> {
    return this.http.put<ListItem>(`${this.baseUrl}/lists/items/${id}`, request);
  }

  // Songs
  getSongsByBundle(bundleId: string, page = 1, pageSize = 50): Observable<PaginatedResult<Song>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PaginatedResult<Song>>(`${this.baseUrl}/songs/bundle/${bundleId}`, { params });
  }

  createSong(request: { bundleId: string; number: number; title?: string; numberOfVerses?: number }): Observable<Song> {
    return this.http.post<Song>(`${this.baseUrl}/songs`, request);
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

  // Export
  exportExcel(request: any): Observable<Blob> {
    return this.http.post(`${this.baseUrl}/export/excel`, request, { responseType: 'blob' });
  }
}
