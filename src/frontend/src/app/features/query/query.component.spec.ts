import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { QueryComponent } from './query.component';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/auth/auth.service';

describe('QueryComponent denominations multi-select', () => {
  let component: QueryComponent;

  beforeEach(() => {
    const api = {
      getTemplates: vi.fn().mockReturnValue(of([])),
      getListByName: vi.fn().mockReturnValue(of({ id: '', name: '', description: null, isSystemList: false, items: [] })),
      getAiStatus: vi.fn().mockReturnValue(of(null)),
      getRecentSearches: vi.fn().mockReturnValue(of([])),
    };
    const auth = { isAuthenticated: false };

    TestBed.configureTestingModule({
      imports: [QueryComponent],
      providers: [
        provideHttpClient(),
        { provide: ApiService, useValue: api },
        { provide: AuthService, useValue: auth },
      ],
    });

    const fixture = TestBed.createComponent(QueryComponent);
    component = fixture.componentInstance;
  });

  it('returns a STABLE array reference while the selection string is unchanged', () => {
    const param = { name: 'denominationIds' };
    component.templateParams['denominationIds'] = 'a,b';

    const first = component.selectedDenominations(param);
    const second = component.selectedDenominations(param);

    // A fresh array on every call would drive mat-select[multiple] into an
    // infinite change-detection loop that hard-hangs the browser.
    expect(second).toBe(first);
    expect(first).toEqual(['a', 'b']);
  });

  it('returns a new array only after the underlying selection changes', () => {
    const param = { name: 'denominationIds' };
    component.templateParams['denominationIds'] = 'a';
    const before = component.selectedDenominations(param);

    component.onDenominationsChange(param, ['a', 'c']);
    const after = component.selectedDenominations(param);

    expect(after).not.toBe(before);
    expect(after).toEqual(['a', 'c']);
    expect(component.templateParams['denominationIds']).toBe('a,c');
  });

  it('treats an empty/absent selection as an empty array', () => {
    const param = { name: 'denominationIds' };
    expect(component.selectedDenominations(param)).toEqual([]);

    component.onDenominationsChange(param, []);
    expect(component.templateParams['denominationIds']).toBe('');
    expect(component.selectedDenominations(param)).toEqual([]);
  });
});
