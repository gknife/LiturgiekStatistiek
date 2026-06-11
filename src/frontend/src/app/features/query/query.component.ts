import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ResultChartComponent, ChartData } from '../../shared/components/result-chart/result-chart.component';
import { ResultTableComponent } from '../../shared/components/result-table/result-table.component';
import { ResultMapComponent } from '../../shared/components/result-map/result-map.component';

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
    MatInputModule, MatFormFieldModule, MatSelectModule, MatProgressSpinnerModule,
    FormsModule, ResultChartComponent, ResultTableComponent, ResultMapComponent,
  ],
  templateUrl: './query.component.html',
  styleUrl: './query.component.scss',
})
export class QueryComponent implements OnInit {
  naturalLanguageQuery = '';
  queryTemplates: QueryTemplate[] = [];
  selectedTemplate: QueryTemplate | null = null;
  templateParams: Record<string, string> = {};
  loading = false;
  result: QueryResult | null = null;

  exampleQueries = [
    'Welk lied wordt het meest gezongen in de GG?',
    'Welk couplet van Psalm 119 wordt het vaakst gezongen?',
    'Vergelijk psalmengebruik tussen PKN en NGK',
    'Welke stad zingt Psalm 150 het meest?',
    'Geef alle diensten waar Psalm 6 gezongen wordt',
    'Hoe is het gebruik van Psalm 116 veranderd over de jaren?',
  ];

  private apiUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<QueryTemplate[]>(`${this.apiUrl}/queries/templates`).subscribe({
      next: templates => this.queryTemplates = templates,
    });
  }

  selectTemplate(template: QueryTemplate): void {
    this.selectedTemplate = template;
    this.templateParams = {};
    template.parameters.forEach(p => {
      if (p.defaultValue) this.templateParams[p.name] = p.defaultValue;
    });
  }

  executeTemplate(): void {
    if (!this.selectedTemplate) return;
    this.loading = true;
    this.result = null;

    this.http.post<QueryResult>(`${this.apiUrl}/queries/execute`, {
      templateId: this.selectedTemplate.id,
      parameters: this.templateParams,
    }).subscribe({
      next: res => { this.result = res; this.loading = false; },
      error: () => { this.loading = false; },
    });
  }

  submitNaturalLanguageQuery(): void {
    if (!this.naturalLanguageQuery) return;
    this.loading = true;
    this.result = null;

    this.http.post<QueryResult>(`${this.apiUrl}/queries/execute`, {
      naturalLanguageQuery: this.naturalLanguageQuery,
    }).subscribe({
      next: res => { this.result = res; this.loading = false; },
      error: () => { this.loading = false; },
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
      a.download = `${this.result?.title || 'export'}.xlsx`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }
}
