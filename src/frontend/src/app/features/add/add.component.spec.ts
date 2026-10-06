import { TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { AddComponent } from './add.component';
import { ApiService } from '../../core/services/api.service';

describe('AddComponent save on close', () => {
  let component: AddComponent;
  let api: {
    createService: ReturnType<typeof vi.fn>;
    updateService: ReturnType<typeof vi.fn>;
  };
  let dialogRef: { close: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    api = {
      createService: vi.fn().mockReturnValue(of({ id: 'saved-service', statusValue: 0 })),
      updateService: vi.fn().mockReturnValue(of({ id: 'saved-service', statusValue: 0 })),
    };
    dialogRef = { close: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AddComponent],
      providers: [
        { provide: ApiService, useValue: {
          ...api,
          getTemplates: vi.fn().mockReturnValue(of([])),
          getListByName: vi.fn().mockReturnValue(of({ items: [] })),
          getBibleBooks: vi.fn().mockReturnValue(of([])),
          searchCongregations: vi.fn().mockReturnValue(of([])),
          searchPreachers: vi.fn().mockReturnValue(of([])),
        } },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    });

    const fixture = TestBed.createComponent(AddComponent);
    component = fixture.componentInstance;
    component.ngOnInit();
    component.metadataForm.patchValue({
      date: new Date(2026, 9, 6),
      congregationId: '00000000-0000-0000-0000-000000000001',
    }, { emitEvent: false });
  });

  it('saves pending changes as a concept before closing', () => {
    component.metadataForm.patchValue({ sermonTheme: 'Een nieuw thema' }, { emitEvent: false });

    component.cancel();

    expect(api.createService).toHaveBeenCalledOnce();
    expect(api.createService.mock.calls[0][0].status).toBe(0);
    expect(dialogRef.close).toHaveBeenCalledWith(true);
  });

  it('waits for an in-flight autosave and saves edits made during it before closing', () => {
    const inFlightSave = new Subject<{ id: string; statusValue: number }>();
    api.createService.mockReturnValue(inFlightSave);
    vi.useFakeTimers();
    component.metadataForm.patchValue({ sermonTheme: 'Eerste versie' }, { emitEvent: false });
    component.scheduleAutosave();
    vi.advanceTimersByTime(2000);
    component.metadataForm.patchValue({ sermonTheme: 'Laatste versie' }, { emitEvent: false });

    component.cancel();
    expect(dialogRef.close).not.toHaveBeenCalled();

    inFlightSave.next({ id: 'saved-service', statusValue: 0 });
    inFlightSave.complete();

    expect(api.updateService).toHaveBeenCalledOnce();
    expect(api.updateService.mock.calls[0][1].sermonTheme).toBe('Laatste versie');
    expect(dialogRef.close).toHaveBeenCalledWith(true);
    vi.useRealTimers();
  });

  it('keeps the dialog open and reports a failed save instead of discarding changes', () => {
    api.createService.mockReturnValue(throwError(() => new Error('offline')));
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});
    component.metadataForm.patchValue({ sermonTheme: 'Een nieuw thema' }, { emitEvent: false });

    component.cancel();

    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(component.autosaveFailed).toBe(true);
    expect(alertSpy).toHaveBeenCalledWith(expect.stringContaining('dienst blijft geopend'));
    alertSpy.mockRestore();
  });
});

describe('AddComponent.canonicalizeVerses', () => {
  it('normalizes spacing to comma+space and trims items', () => {
    expect(AddComponent.canonicalizeVerses('1,2 ,  3')).toBe('1, 2, 3');
  });

  it('collapses ranges to hyphen without spaces', () => {
    expect(AddComponent.canonicalizeVerses('1, 5 - 7')).toBe('1, 5-7');
  });

  it('accepts en-dash ranges and normalizes them to hyphen', () => {
    expect(AddComponent.canonicalizeVerses('5 – 7')).toBe('5-7');
  });

  it('drops empty items produced by trailing/duplicate commas', () => {
    expect(AddComponent.canonicalizeVerses('1,,3,')).toBe('1, 3');
  });

  it('preserves the original order', () => {
    expect(AddComponent.canonicalizeVerses('3, 1, 2')).toBe('3, 1, 2');
  });

  it('keeps invalid tokens verbatim so the user can correct them', () => {
    expect(AddComponent.canonicalizeVerses('1, refrein, 3')).toBe('1, refrein, 3');
  });

  it('returns an empty string for empty input', () => {
    expect(AddComponent.canonicalizeVerses('')).toBe('');
  });
});
