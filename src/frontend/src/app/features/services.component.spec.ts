import { TestBed } from '@angular/core/testing';
import { ServicesComponent } from './services.component';
import { ApiService } from '../core/services/api.service';
import { AuthService } from '../core/auth/auth.service';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';

describe('ServicesComponent', () => {
  let component: ServicesComponent;

  const apiMock = {
    getServices: () => of({ items: [], totalCount: 0, page: 1, pageSize: 20 }),
    getCongregations: () => of([]),
    getListByName: () => of({ items: [] }),
  } as unknown as ApiService;

  const authMock = {
    isAuthenticated: false,
  } as unknown as AuthService;

  const dialogMock = { open: () => ({ afterClosed: () => of(false) }) } as unknown as MatDialog;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServicesComponent],
      providers: [
        { provide: ApiService, useValue: apiMock },
        { provide: AuthService, useValue: authMock },
        { provide: MatDialog, useValue: dialogMock },
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(ServicesComponent);
    component = fixture.componentInstance;
  });

  it('maps TimeOfDay enum names to Dutch labels', () => {
    expect(component.timeLabel('Morning')).toBe('Morgen');
    expect(component.timeLabel('Afternoon')).toBe('Middag');
    expect(component.timeLabel('Evening')).toBe('Avond');
    expect(component.timeLabel('Unknown')).toBe('Unknown');
  });

  it('toggles selection of a service id', () => {
    expect(component.isSelected('a')).toBe(false);
    component.toggleSelect('a');
    expect(component.isSelected('a')).toBe(true);
    component.toggleSelect('a');
    expect(component.isSelected('a')).toBe(false);
  });

  it('hides edit controls for anonymous users', () => {
    expect(component.isAuthenticated).toBe(false);
    expect(component.isAdmin).toBe(false);
  });

  it('formats multi-part reading references into a compact label', () => {
    const el = {
      readingReferences: [
        { bookName: 'Genesis', chapter: 1, verseStart: 1, verseEnd: 3, bibleBookId: null, position: 1 },
        { bookName: 'Psalm', chapter: 23, verseStart: 1, verseEnd: 1, bibleBookId: null, position: 2 },
      ],
    } as any;
    expect(component.readingRefsLabel(el)).toBe('Genesis 1:1-3; Psalm 23:1');
  });

  it('returns empty string when there are no reading references', () => {
    expect(component.readingRefsLabel({ readingReferences: null } as any)).toBe('');
  });
});
