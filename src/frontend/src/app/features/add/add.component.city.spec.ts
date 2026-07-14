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

  const existing: PreacherSummary = { id: 'p1', fullName: 'Ds. Bestaand', title: null, city: 'Ede' };

  it('shows an existing preacher city read-only when selected', () => {
    component.selectPreacher(existing);

    expect(component.preacherCityControl.value).toBe('Ede');
    expect(component.preacherCityControl.disabled).toBe(true);
    expect(component.metadataForm.value.preacherId).toBe('p1');
  });

  it('re-enables and clears the preacher id when a new name is typed', () => {
    component.selectPreacher(existing);
    expect(component.preacherCityControl.disabled).toBe(true);

    component.preacherControl.setValue('Ds. Nieuw');

    expect(component.preacherCityControl.enabled).toBe(true);
    expect(component.metadataForm.value.preacherId).toBe('');
  });

  it('includes the preacher city in the autosave snapshot', () => {
    component.preacherCityControl.setValue('Barneveld');
    const snapshot = JSON.parse((component as unknown as { snapshot(): string }).snapshot());
    expect(snapshot.preacherCity).toBe('Barneveld');
  });
});
