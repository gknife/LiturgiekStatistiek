import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AddComponent } from './add.component';
import { ApiService } from '../../core/services/api.service';
import { ListItem } from '../../core/models/api.models';

function label(id: string, value: string, type: number | null): ListItem {
  return {
    id,
    value,
    abbreviation: null,
    sortOrder: 0,
    isActive: true,
    liturgicalElementType: type,
  };
}

describe('AddComponent onderdeel dropdowns', () => {
  let component: AddComponent;

  beforeEach(() => {
    const api = {
      getTemplates: vi.fn().mockReturnValue(of([])),
      getListByName: vi.fn().mockReturnValue(of({ id: '', name: '', description: null, isSystemList: false, items: [] })),
      getBibleBooks: vi.fn().mockReturnValue(of([])),
      searchCongregations: vi.fn().mockReturnValue(of([])),
      searchPreachers: vi.fn().mockReturnValue(of([])),
    };

    TestBed.configureTestingModule({
      imports: [AddComponent],
      providers: [{ provide: ApiService, useValue: api }],
    });

    const fixture = TestBed.createComponent(AddComponent);
    component = fixture.componentInstance;
    component.ngOnInit();
  });

  it('shows only classified labels matching the type, plus unclassified ones', () => {
    component.liturgicalLabels = [
      label('song', 'Openingslied', 0),
      label('reading', 'Schriftlezing', 2),
      label('prayer', 'Gebed', 3),
      label('free', 'Vrij onderdeel', null),
    ];

    const forReading = component.labelsForType(2).map(l => l.id);

    expect(forReading).toContain('reading');
    expect(forReading).toContain('free');
    expect(forReading).not.toContain('song');
    expect(forReading).not.toContain('prayer');
  });

  it('auto-selects the label when exactly one classified label matches the type', () => {
    component.liturgicalLabels = [
      label('song', 'Openingslied', 0),
      label('reading', 'Schriftlezing', 2),
      label('free', 'Vrij onderdeel', null),
    ];
    const element = { position: 1, elementType: 2, labelId: '', songs: [], notes: '', performerId: '', isBeurtzang: false, bibleTranslationId: '', readingRefs: [] };

    component.onElementTypeChange(element as never);

    expect((element as { labelId: string }).labelId).toBe('reading');
  });

  it('does not auto-select when multiple classified labels match the type', () => {
    component.liturgicalLabels = [
      label('reading1', 'OT-lezing', 2),
      label('reading2', 'NT-lezing', 2),
    ];
    const element = { position: 1, elementType: 2, labelId: '', songs: [], notes: '', performerId: '', isBeurtzang: false, bibleTranslationId: '', readingRefs: [] };

    component.onElementTypeChange(element as never);

    expect((element as { labelId: string }).labelId).toBe('');
  });

  it('clears a selected label that no longer matches after a type change', () => {
    component.liturgicalLabels = [
      label('song1', 'Lied A', 0),
      label('song2', 'Lied B', 0),
      label('reading', 'Schriftlezing', 2),
    ];
    const element = { position: 1, elementType: 2, labelId: 'song1', songs: [], notes: '', performerId: '', isBeurtzang: false, bibleTranslationId: '', readingRefs: [] };

    component.onElementTypeChange(element as never);

    expect((element as { labelId: string }).labelId).not.toBe('song1');
  });

  it('honours the template performer, including an explicit "geen", instead of defaulting to Voorganger', () => {
    // A Voorganger performer exists, so the old behaviour would have defaulted to it.
    component.performers = [
      { id: 'voorganger-id', value: 'Voorganger', abbreviation: null, sortOrder: 0, isActive: true, liturgicalElementType: null },
      { id: 'organist-id', value: 'Organist', abbreviation: null, sortOrder: 1, isActive: true, liturgicalElementType: null },
    ];

    const template = {
      id: 't1',
      name: 'Test',
      elements: [
        // No performer set -> must stay empty (not become Voorganger).
        { id: 'e1', position: 1, elementType: 'LiturgicalAction', elementTypeValue: 1, labelId: null, label: null, performerId: null, performer: null, isBeurtzang: false, fixedScriptureReference: null },
        // Explicit performer -> must be preserved.
        { id: 'e2', position: 2, elementType: 'LiturgicalAction', elementTypeValue: 1, labelId: null, label: null, performerId: 'organist-id', performer: 'Organist', isBeurtzang: false, fixedScriptureReference: null },
      ],
    };

    (component as never as { applyTemplateDto(t: unknown): void }).applyTemplateDto(template);

    expect(component.elements[0].performerId).toBe('');
    expect(component.elements[1].performerId).toBe('organist-id');
  });
});
