import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../core/services/api.service';
import { ListItem, Song } from '../../core/models/api.models';
import { environment } from '../../../environments/environment';
import { ResultChartComponent, ChartData } from '../../shared/components/result-chart/result-chart.component';
import { ResultTableComponent } from '../../shared/components/result-table/result-table.component';
import { ResultMapComponent } from '../../shared/components/result-map/result-map.component';
import { AdvancedQueryComponent } from './advanced-query.component';
import { AiStatus } from '../../core/models/api.models';

interface QueryTemplate {
  id: string;
  title: string;
  description: string;
  parameters: { name: string; label: string; type: string; required: boolean; defaultValue?: string }[];
  defaultChartType: string;
}

interface QueryResult {
  title: string;
  description: string;
  chartType: string;
  columns: string[];
  rows: Record<string, any>[];
  totalCount: number;
  chart?: ChartData;
}

@Component({
  selector: 'app-query',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatTabsModule, MatIconModule, MatButtonModule,
    MatInputModule, MatFormFieldModule, MatSelectModule, MatAutocompleteModule,
    MatProgressSpinnerModule,
    FormsModule, ResultChartComponent, ResultTableComponent, ResultMapComponent,
    AdvancedQueryComponent,
  ],
  templateUrl: './query.component.html',
  styleUrl: './query.component.scss',
})
export class QueryComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiService);

  naturalLanguageQuery = '';
  readonly queryTemplates = signal<QueryTemplate[]>([]);
  selectedTemplate: QueryTemplate | null = null;
  templateParams: Record<string, string> = {};
  readonly loading = signal(false);
  readonly result = signal<QueryResult | null>(null);
  readonly aiStatus = signal<AiStatus | null>(null);

  readonly congregations = signal<{ id: string; label: string }[]>([]);
  readonly bundles = signal<ListItem[]>([]);
  readonly songsByBundle = signal<Record<string, Song[]>>({});
  readonly songVerses = signal<Record<string, { number: number; title: string | null }[]>>({});
  readonly years = Array.from({ length: 30 }, (_, i) => (new Date().getFullYear() - i).toString());
  readonly months = [
    { value: '1', label: 'Januari' }, { value: '2', label: 'Februari' },
    { value: '3', label: 'Maart' }, { value: '4', label: 'April' },
    { value: '5', label: 'Mei' }, { value: '6', label: 'Juni' },
    { value: '7', label: 'Juli' }, { value: '8', label: 'Augustus' },
    { value: '9', label: 'September' }, { value: '10', label: 'Oktober' },
    { value: '11', label: 'November' }, { value: '12', label: 'December' },
  ];

  exampleQueries = [
    'Welk lied wordt het meest gezongen in de GG?',
    'Welk couplet van Psalm 119 wordt het vaakst gezongen?',
    'Vergelijk psalmengebruik tussen PKN en NGK',
    'Welke stad zingt Psalm 150 het meest?',
    'Geef alle diensten waar Psalm 6 gezongen wordt',
    'Hoe is het gebruik van Psalm 116 veranderd over de jaren?',
  ];

  private apiUrl = environment.apiUrl;

  ngOnInit(): void {
    this.http.get<QueryTemplate[]>(`${this.apiUrl}/queries/templates`).subscribe({
      next: templates => this.queryTemplates.set(templates),
    });

    this.http.get<AiStatus>(`${this.apiUrl}/queries/ai-status`).subscribe({
      next: status => this.aiStatus.set(status),
    });

    this.api.getCongregations({ page: 1, pageSize: 500 }).subscribe({
      next: res => this.congregations.set(
        res.items
          .map(c => ({ id: c.id, label: c.city ? `${c.name} — ${c.city}` : c.name }))
          .sort((a, b) => a.label.localeCompare(b.label, 'nl')),
      ),
    });

    this.api.getListByName('SongBundles').subscribe({
      next: list => this.bundles.set(list.items),
    });
  }

  selectTemplate(template: QueryTemplate): void {
    this.selectedTemplate = template;
    this.templateParams = {};
    template.parameters.forEach(p => {
      if (p.defaultValue) this.templateParams[p.name] = p.defaultValue;
    });
    template.parameters
      .filter(p => p.type === 'bundle' && this.templateParams[p.name])
      .forEach(p => this.loadSongsForBundle(this.templateParams[p.name]));
  }

  isCongregation(p: { type: string }): boolean { return p.type === 'congregation'; }
  isBundle(p: { type: string }): boolean { return p.type === 'bundle'; }
  isDate(p: { type: string }): boolean { return p.type === 'date'; }
  isVerse(p: { type: string }): boolean { return p.type === 'verse'; }
  isSongNumber(p: { name: string }): boolean { return p.name.toLowerCase().includes('songnumber'); }
  isYear(p: { name: string }): boolean { return p.name === 'year'; }
  isMonth(p: { name: string }): boolean { return p.name === 'month'; }

  isPlainText(p: { name: string; type: string }): boolean {
    return !this.isCongregation(p) && !this.isBundle(p) && !this.isDate(p)
      && !this.isSongNumber(p) && !this.isYear(p) && !this.isMonth(p) && !this.isVerse(p);
  }

  private bundleParamFor(p: { name: string }): string {
    return p.name.replace(/songNumber/i, 'bundleId');
  }

  private sectionParamFor(p: { name: string }): string {
    return p.name.replace(/songNumber/i, 'section');
  }

  sectionParamName(p: { name: string }): string {
    return this.sectionParamFor(p);
  }

  private verseKey(bundleId: string, section: string, number: number): string {
    return `${bundleId}:${section}:${number}`;
  }

  loadSongsForBundle(bundleId: string): void {
    if (!bundleId || this.songsByBundle()[bundleId]) return;
    this.api.getSongsByBundle(bundleId, 1, 2000).subscribe({
      next: res => this.songsByBundle.update(m => ({ ...m, [bundleId]: res.items })),
    });
  }

  onBundleParamChange(p: { name: string }): void {
    const suffix = p.name.replace(/bundleId/i, '');
    delete this.templateParams['section' + suffix];
    delete this.templateParams['songNumber' + suffix];
    delete this.templateParams['verse' + suffix];
    this.loadSongsForBundle(this.templateParams[p.name]);
  }

  /** Distinct, non-empty sections (e.g. Psalm / Gezang) for the bundle tied to a songNumber param. */
  sectionsFor(p: { name: string }): string[] {
    const bundleId = this.templateParams[this.bundleParamFor(p)];
    const songs = bundleId ? (this.songsByBundle()[bundleId] || []) : [];
    return Array.from(new Set(songs.map(s => s.section).filter(s => !!s))).sort((a, b) => a.localeCompare(b, 'nl'));
  }

  hasSections(p: { name: string }): boolean {
    return this.sectionsFor(p).length > 1;
  }

  onSectionChange(p: { name: string }): void {
    const suffix = p.name.replace(/songNumber/i, '');
    delete this.templateParams['songNumber' + suffix];
    delete this.templateParams['verse' + suffix];
  }

  onSongNumberChange(p: { name: string }): void {
    const bundleId = this.templateParams[this.bundleParamFor(p)];
    const section = this.templateParams[this.sectionParamFor(p)] || '';
    const suffix = p.name.replace(/songNumber/i, '');
    delete this.templateParams['verse' + suffix];

    const num = parseInt((this.templateParams[p.name] || '').toString(), 10);
    if (!bundleId || isNaN(num)) return;
    const key = this.verseKey(bundleId, section, num);
    if (this.songVerses()[key]) return;

    const songs = this.songsByBundle()[bundleId] || [];
    const match = songs.find(s => s.number === num && (section ? s.section === section : true));
    const load$ = match
      ? this.api.getSong(match.id)
      : this.api.getSongByNumber(bundleId, num);
    load$.subscribe({
      next: song => this.songVerses.update(m => ({ ...m, [key]: song.verses ?? [] })),
      error: () => this.songVerses.update(m => ({ ...m, [key]: [] })),
    });
  }

  versesFor(p: { name: string }): { number: number; title: string | null }[] {
    const suffix = p.name.replace(/verse/i, '');
    const bundleId = this.templateParams['bundleId' + suffix];
    const section = this.templateParams['section' + suffix] || '';
    const num = parseInt((this.templateParams['songNumber' + suffix] || '').toString(), 10);
    if (!bundleId || isNaN(num)) return [];
    return this.songVerses()[this.verseKey(bundleId, section, num)] || [];
  }

  filteredSongs(p: { name: string }): Song[] {
    const bundleId = this.templateParams[this.bundleParamFor(p)];
    const section = this.templateParams[this.sectionParamFor(p)] || '';
    let songs = bundleId ? (this.songsByBundle()[bundleId] || []) : [];
    if (section) songs = songs.filter(s => s.section === section);
    const q = (this.templateParams[p.name] || '').toString().toLowerCase();
    const matches = q
      ? songs.filter(s => s.number.toString().includes(q) || (s.title || '').toLowerCase().includes(q))
      : songs;
    return matches.slice(0, 50);
  }


  clearTemplate(): void {
    this.selectedTemplate = null;
    this.result.set(null);
  }

  executeTemplate(): void {
    if (!this.selectedTemplate) return;
    this.loading.set(true);
    this.result.set(null);

    this.http.post<QueryResult>(`${this.apiUrl}/queries/execute`, {
      templateId: this.selectedTemplate.id,
      parameters: this.templateParams,
    }).subscribe({
      next: res => { this.result.set(res); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  submitNaturalLanguageQuery(): void {
    if (!this.naturalLanguageQuery) return;
    this.loading.set(true);
    this.result.set(null);

    this.http.post<QueryResult>(`${this.apiUrl}/queries/execute`, {
      naturalLanguageQuery: this.naturalLanguageQuery,
    }).subscribe({
      next: res => { this.result.set(res); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  useExample(query: string): void {
    this.naturalLanguageQuery = query;
    this.submitNaturalLanguageQuery();
  }

  exportExcel(): void {
    if (!this.selectedTemplate) return;
    this.http.post(`${this.apiUrl}/export/excel`, {
      templateId: this.selectedTemplate.id,
      parameters: this.templateParams,
    }, { responseType: 'blob' }).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${this.result()?.title || 'export'}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }
}
