import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import {
  ServiceTemplateSummary,
  ServiceTemplate,
  CreateServiceTemplateRequest,
  CreateServiceTemplateElementRequest,
  ListItem,
  CongregationSummary,
} from '../../core/models/api.models';

interface EditableElement {
  position: number;
  elementType: number;
  labelId: string | null;
  performerId: string | null;
  isBeurtzang: boolean;
  fixedScriptureReference: string | null;
}

@Component({
  selector: 'app-templates',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatTableModule, MatIconModule,
    MatButtonModule, MatInputModule, MatFormFieldModule, MatSelectModule,
    MatCheckboxModule, MatSlideToggleModule, MatTooltipModule, MatSnackBarModule,
  ],
  templateUrl: './templates.component.html',
  styleUrl: './templates.component.scss',
})
export class TemplatesComponent implements OnInit {
  readonly templates = signal<ServiceTemplateSummary[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);

  denominations: ListItem[] = [];
  occasions: ListItem[] = [];
  performers: ListItem[] = [];
  labels: ListItem[] = [];
  congregations: CongregationSummary[] = [];

  readonly displayedColumns = ['name', 'denomination', 'timeOfDay', 'occasion', 'elementCount', 'active', 'actions'];

  readonly elementTypes = [
    { value: 0, label: 'Lied' },
    { value: 1, label: 'Liturgische handeling' },
    { value: 2, label: 'Schriftlezing' },
    { value: 3, label: 'Gebed' },
    { value: 4, label: 'Overig' },
  ];

  readonly timesOfDay = [
    { value: 0, label: 'Morgen' },
    { value: 1, label: 'Middag' },
    { value: 2, label: 'Avond' },
  ];

  // Editor state
  editing = false;
  editingId: string | null = null;
  form: CreateServiceTemplateRequest = this.emptyForm();
  elements: EditableElement[] = [];

  constructor(private api: ApiService, private snackBar: MatSnackBar) {}

  ngOnInit(): void {
    forkJoin({
      denominations: this.api.getListByName('Denominations'),
      occasions: this.api.getListByName('ServiceOccasion'),
      performers: this.api.getListByName('ServicePerformer'),
      labels: this.api.getListByName('LiturgicalLabels'),
    }).subscribe({
      next: (lists) => {
        this.denominations = lists.denominations.items;
        this.occasions = lists.occasions.items;
        this.performers = lists.performers.items;
        this.labels = lists.labels.items;
        this.loadTemplates();
      },
      error: () => {
        this.snackBar.open('Kon lijsten niet laden', 'Sluiten', { duration: 4000 });
        this.loadTemplates();
      },
    });
  }

  private loadTemplates(): void {
    this.loading.set(true);
    this.api.getTemplates().subscribe({
      next: (t) => { this.templates.set(t); this.loading.set(false); },
      error: () => { this.loading.set(false); },
    });
  }

  private emptyForm(): CreateServiceTemplateRequest {
    return {
      name: '',
      denominationId: null,
      congregationId: null,
      timeOfDay: null,
      occasionId: null,
      isActive: true,
      elements: [],
    };
  }

  timeLabel(value: number | null): string {
    return this.timesOfDay.find(t => t.value === value)?.label ?? 'Elke';
  }

  elementTypeLabel(value: number): string {
    return this.elementTypes.find(t => t.value === value)?.label ?? '';
  }

  startCreate(): void {
    this.editing = true;
    this.editingId = null;
    this.form = this.emptyForm();
    this.elements = [];
  }

  startEdit(summary: ServiceTemplateSummary): void {
    this.api.getTemplate(summary.id).subscribe((t: ServiceTemplate) => {
      this.editing = true;
      this.editingId = t.id;
      this.form = {
        name: t.name,
        denominationId: t.denominationId,
        congregationId: t.congregationId,
        timeOfDay: t.timeOfDay,
        occasionId: t.occasionId,
        isActive: t.isActive,
        elements: [],
      };
      this.elements = t.elements.map(e => ({
        position: e.position,
        elementType: e.elementTypeValue,
        labelId: e.labelId,
        performerId: e.performerId,
        isBeurtzang: e.isBeurtzang,
        fixedScriptureReference: e.fixedScriptureReference,
      }));
    });
  }

  cancel(): void {
    this.editing = false;
    this.editingId = null;
  }

  addElement(): void {
    this.elements.push({
      position: this.elements.length,
      elementType: 1,
      labelId: null,
      performerId: null,
      isBeurtzang: false,
      fixedScriptureReference: null,
    });
  }

  removeElement(index: number): void {
    this.elements.splice(index, 1);
    this.elements.forEach((e, i) => (e.position = i));
  }

  moveElement(index: number, delta: number): void {
    const target = index + delta;
    if (target < 0 || target >= this.elements.length) return;
    const [item] = this.elements.splice(index, 1);
    this.elements.splice(target, 0, item);
    this.elements.forEach((e, i) => (e.position = i));
  }

  isSong(el: EditableElement): boolean {
    return el.elementType === 0;
  }

  isReading(el: EditableElement): boolean {
    return el.elementType === 2;
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.snackBar.open('Naam is verplicht', 'Sluiten', { duration: 3000 });
      return;
    }
    if (this.saving()) return;
    this.saving.set(true);

    const request: CreateServiceTemplateRequest = {
      ...this.form,
      elements: this.elements.map<CreateServiceTemplateElementRequest>((e, i) => ({
        position: i,
        elementType: e.elementType,
        labelId: e.labelId,
        performerId: this.isSong(e) ? null : e.performerId,
        isBeurtzang: this.isSong(e) ? e.isBeurtzang : false,
        fixedScriptureReference: e.fixedScriptureReference,
      })),
    };

    const obs = this.editingId
      ? this.api.updateTemplate(this.editingId, request)
      : this.api.createTemplate(request);

    obs.subscribe({
      next: () => {
        this.saving.set(false);
        this.editing = false;
        this.snackBar.open('Sjabloon opgeslagen', 'Sluiten', { duration: 2500 });
        this.loadTemplates();
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Opslaan mislukt', 'Sluiten', { duration: 4000 });
      },
    });
  }

  deleteTemplate(summary: ServiceTemplateSummary): void {
    if (!confirm(`Sjabloon "${summary.name}" verwijderen?`)) return;
    this.api.deleteTemplate(summary.id).subscribe({
      next: () => { this.snackBar.open('Sjabloon verwijderd', 'Sluiten', { duration: 2500 }); this.loadTemplates(); },
      error: () => { this.snackBar.open('Verwijderen mislukt', 'Sluiten', { duration: 4000 }); },
    });
  }
}
