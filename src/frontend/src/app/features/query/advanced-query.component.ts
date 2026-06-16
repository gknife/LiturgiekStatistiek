import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/auth/auth.service';
import {
  AdvancedField,
  AdvancedQueryDefinition,
  AdvancedQuerySchema,
  QueryResult,
  SavedQuery,
} from '../../core/models/api.models';
import { ResultChartComponent } from '../../shared/components/result-chart/result-chart.component';
import { ResultTableComponent } from '../../shared/components/result-table/result-table.component';

interface BuilderFilter {
  field: string;
  operator: string;
  value?: string;
  value2?: string;
  songBundleA?: string;
  songNumberA?: number;
  songBundleB?: string;
  songNumberB?: number;
}

interface BuilderQuery {
  name: string;
  filters: BuilderFilter[];
  outputMode: 'list' | 'aggregate';
  groupBy: string;
  chartType: string;
}

interface Option {
  id: string;
  label: string;
}

@Component({
  selector: 'app-advanced-query',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatIconModule, MatButtonModule,
    MatInputModule, MatFormFieldModule, MatSelectModule, MatCheckboxModule,
    MatProgressSpinnerModule, MatTooltipModule,
    ResultChartComponent, ResultTableComponent,
  ],
  templateUrl: './advanced-query.component.html',
  styleUrl: './advanced-query.component.scss',
})
export class AdvancedQueryComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);

  readonly schema = signal<AdvancedQuerySchema | null>(null);
  readonly loading = signal(false);
  readonly result = signal<QueryResult | null>(null);
  readonly savedQueries = signal<SavedQuery[]>([]);
  readonly congregations = signal<Option[]>([]);
  readonly preachers = signal<Option[]>([]);

  readonly chartTypes = ['bar', 'line', 'pie', 'doughnut'];
  readonly timeOfDayOptions = [
    { value: '0', label: 'Morgen' },
    { value: '1', label: 'Middag' },
    { value: '2', label: 'Avond' },
  ];

  // Builder state is mutated through template events (zoneless-safe).
  queries: BuilderQuery[] = [this.newQuery('Query 1')];
  activeIndex = 0;

  get isAuthenticated(): boolean {
    return this.auth.isAuthenticated;
  }

  get activeQuery(): BuilderQuery {
    return this.queries[this.activeIndex];
  }

  ngOnInit(): void {
    this.api.getAdvancedSchema().subscribe({
      next: s => this.schema.set(s),
    });

    this.api.getCongregations({ pageSize: 1000 }).subscribe({
      next: res => this.congregations.set(
        res.items.map(c => ({ id: c.id, label: `${c.name} (${c.city})` }))),
    });

    this.api.getPreachers({ pageSize: 1000 }).subscribe({
      next: res => this.preachers.set(
        res.items.map(p => ({ id: p.id, label: p.fullName }))),
    });

    if (this.isAuthenticated) {
      this.refreshSavedQueries();
    }
  }

  private newQuery(name: string): BuilderQuery {
    return { name, filters: [], outputMode: 'list', groupBy: 'congregation', chartType: 'bar' };
  }

  fieldDef(key: string): AdvancedField | undefined {
    return this.schema()?.fields.find(f => f.key === key);
  }

  fieldType(key: string): string {
    return this.fieldDef(key)?.type ?? 'text';
  }

  operatorsFor(key: string): string[] {
    return this.fieldDef(key)?.operators ?? [];
  }

  operatorLabel(op: string): string {
    const labels: Record<string, string> = {
      eq: 'is gelijk aan', neq: 'is niet', in: 'is een van', contains: 'bevat',
      between: 'tussen', before: 'voor', after: 'na', isTrue: 'ja', isFalse: 'nee',
      seqBefore: 'ergens voor', seqAfter: 'ergens na',
      seqDirectlyBefore: 'direct voor', seqDirectlyAfter: 'direct na',
    };
    return labels[op] ?? op;
  }

  // --- Filter management ---
  addFilter(): void {
    const firstField = this.schema()?.fields[0];
    if (!firstField) return;
    this.activeQuery.filters.push({
      field: firstField.key,
      operator: firstField.operators[0] ?? 'eq',
    });
  }

  removeFilter(index: number): void {
    this.activeQuery.filters.splice(index, 1);
  }

  onFieldChange(filter: BuilderFilter): void {
    const ops = this.operatorsFor(filter.field);
    filter.operator = ops[0] ?? 'eq';
    filter.value = undefined;
    filter.value2 = undefined;
    filter.songBundleA = undefined;
    filter.songNumberA = undefined;
    filter.songBundleB = undefined;
    filter.songNumberB = undefined;
  }

  // --- Query management ---
  addQuery(): void {
    this.queries.push(this.newQuery(`Query ${this.queries.length + 1}`));
    this.activeIndex = this.queries.length - 1;
  }

  removeQuery(index: number): void {
    if (this.queries.length === 1) return;
    this.queries.splice(index, 1);
    this.activeIndex = Math.min(this.activeIndex, this.queries.length - 1);
  }

  selectQuery(index: number): void {
    this.activeIndex = index;
  }

  private toDefinition(q: BuilderQuery): AdvancedQueryDefinition {
    return {
      name: q.name,
      filters: q.filters.map(f => ({
        field: f.field,
        operator: f.operator,
        value: f.value ?? null,
        value2: f.value2 ?? null,
        songBundleA: f.songBundleA ?? null,
        songNumberA: f.songNumberA ?? null,
        songBundleB: f.songBundleB ?? null,
        songNumberB: f.songNumberB ?? null,
      })),
      outputMode: q.outputMode,
      groupBy: q.outputMode === 'aggregate' ? q.groupBy : null,
      page: 1,
      pageSize: 50,
      chartType: q.chartType,
    };
  }

  // --- Execution ---
  run(): void {
    this.loading.set(true);
    this.result.set(null);
    this.api.executeAdvancedQuery(this.toDefinition(this.activeQuery)).subscribe({
      next: res => { this.result.set(res); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  compare(): void {
    if (this.queries.length < 2) return;
    this.loading.set(true);
    this.result.set(null);
    this.api.compareAdvancedQueries(this.queries.map(q => this.toDefinition(q))).subscribe({
      next: res => { this.result.set(res); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  exportExcel(): void {
    this.api.exportAdvancedExcel(this.toDefinition(this.activeQuery)).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${this.result()?.title || 'export'}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  // --- Save / load ---
  refreshSavedQueries(): void {
    this.api.getSavedQueries().subscribe({
      next: items => this.savedQueries.set(items),
    });
  }

  saveCurrent(): void {
    if (!this.isAuthenticated) return;
    const payload = JSON.stringify(this.activeQuery);
    this.api.createSavedQuery({
      name: this.activeQuery.name || 'Naamloze query',
      queryParameters: payload,
      isPublic: false,
    }).subscribe({
      next: () => this.refreshSavedQueries(),
    });
  }

  loadSaved(saved: SavedQuery): void {
    try {
      const parsed = JSON.parse(saved.queryParameters) as BuilderQuery;
      parsed.name = saved.name;
      this.queries[this.activeIndex] = {
        name: parsed.name,
        filters: parsed.filters ?? [],
        outputMode: parsed.outputMode ?? 'list',
        groupBy: parsed.groupBy ?? 'congregation',
        chartType: parsed.chartType ?? 'bar',
      };
    } catch {
      // Ignore malformed saved query.
    }
  }

  deleteSaved(saved: SavedQuery): void {
    this.api.deleteSavedQuery(saved.id).subscribe({
      next: () => this.refreshSavedQueries(),
    });
  }
}
