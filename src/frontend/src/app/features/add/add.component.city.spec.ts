import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { AddComponent } from './add.component';
import { ApiService } from '../../core/services/api.service';
import { PreacherSummary } from '../../core/models/api.models';

describe('AddComponent preacher city', () => {
  let component: AddComponent;

  beforeEach(() => {
    const api = {
      getTemplates: vi.fn().mockReturnValue(of([])),
      getListByName: vi.fn().mockReturnValue(of({ id: '', name: '', description: null, isSystemList: false, items: [] })),
      getBibleBooks: vi.fn().mockReturnValue(of([])),
      searchCongregations: vi.fn().mockReturnValue(of([])),
      searchPreachers: vi.fn().mockReturnValue(of([])),
      createPreacher: vi.fn().mockReturnValue(of({ id: 'new' })),
    };

    TestBed.configureTestingModule({
      imports: [AddComponent],
      providers: [{ provide: ApiService, useValue: api }],
    });

    const fixture = TestBed.createComponent(AddComponent);
    component = fixture.componentInstance;
    component.ngOnInit();
  });

  const existing: PreacherSummary = { id: 'p1', fullName: 'Ds. Bestaand', city: 'Ede', title: null, denomination: null };

  it('shows an existing preacher city read-only value when selected', () => {
    component.selectPreacher(existing);

    expect(component.preacherCityControl.value).toBe('Ede');
    expect(component.metadataForm.value.preacherId).toBe('p1');
  });

  it('clears the preacher id and city when a new name is typed (select-only)', () => {
    component.selectPreacher(existing);
    expect(component.metadataForm.value.preacherId).toBe('p1');

    component.preacherControl.setValue('Ds. Nieuw');

    expect(component.metadataForm.value.preacherId).toBe('');
    expect(component.preacherCityControl.value).toBe('');
  });

  it('includes the preacher city in the autosave snapshot', () => {
    component.preacherCityControl.setValue('Barneveld');
    const snapshot = JSON.parse((component as unknown as { snapshot(): string }).snapshot());
    expect(snapshot.preacherCity).toBe('Barneveld');
  });
});
