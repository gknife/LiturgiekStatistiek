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
});
