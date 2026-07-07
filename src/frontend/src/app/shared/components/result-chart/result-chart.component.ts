import { Component, Input, OnChanges, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

export interface ChartData {
  labels: string[];
  datasets: { label: string; data: number[]; backgroundColor?: string | null }[];
}

@Component({
  selector: 'app-result-chart',
  standalone: true,
  template: `<div class="chart-container"><canvas #chartCanvas></canvas></div>`,
  styles: [`.chart-container { position: relative; width: 100%; height: 400px; }`],
})
export class ResultChartComponent implements OnChanges, AfterViewInit {
  @ViewChild('chartCanvas') canvasRef!: ElementRef<HTMLCanvasElement>;
  @Input() chartType: string = 'bar';
  @Input() data: ChartData | null = null;

  private chart: Chart | null = null;
  private initialized = false;

  ngAfterViewInit(): void {
    this.initialized = true;
    this.renderChart();
  }

  ngOnChanges(): void {
    if (this.initialized) this.renderChart();
  }

  private renderChart(): void {
    if (!this.data || !this.canvasRef) return;
    if (this.chart) this.chart.destroy();

    const colors = [
      '#2c5282', '#2f855a', '#744210', '#553c9a', '#c53030',
      '#1a365d', '#285e61', '#975a16', '#6b46c1', '#e53e3e',
    ];

    const datasets = this.data.datasets.map((ds, i) => ({
      ...ds,
      backgroundColor: ds.backgroundColor || colors[i % colors.length],
      borderColor: ds.backgroundColor || colors[i % colors.length],
      tension: 0.3,
    }));

    // Count-based charts must show integer ticks/tooltips (1, 2, 3 — never 1.0/0.5).
    // Detect this when every data point is a whole number; averages/percentages
    // keep decimals because their values are fractional.
    const allValues = this.data.datasets.flatMap(ds => ds.data ?? []);
    const allIntegers = allValues.length > 0 && allValues.every(v => Number.isInteger(v));
    const fmtInt = (v: number | string) =>
      typeof v === 'number' ? v.toLocaleString('nl-NL', { maximumFractionDigits: 0 }) : v;

    this.chart = new Chart(this.canvasRef.nativeElement, {
      type: this.chartType as any,
      data: { labels: this.data.labels, datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: this.data.datasets.length > 1 },
          tooltip: allIntegers ? {
            callbacks: {
              label: (ctx: any) => {
                const label = ctx.dataset?.label ? `${ctx.dataset.label}: ` : '';
                const val = ctx.parsed?.y ?? ctx.parsed ?? ctx.raw;
                return `${label}${fmtInt(val)}`;
              },
            },
          } : {},
        },
        scales: this.chartType !== 'pie' && this.chartType !== 'doughnut' ? {
          y: {
            beginAtZero: true,
            ticks: allIntegers ? {
              precision: 0,
              stepSize: 1,
              callback: (value: any) => (Number.isInteger(value) ? fmtInt(value) : ''),
            } : {},
          },
        } : undefined,
      },
    });
  }

  exportAsImage(): string | null {
    return this.chart?.toBase64Image() ?? null;
  }
}
