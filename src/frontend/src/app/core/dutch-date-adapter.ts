import { Injectable } from '@angular/core';
import { NativeDateAdapter } from '@angular/material/core';
import { MatDateFormats } from '@angular/material/core';

/**
 * Date adapter that reads and writes dates in Dutch `dd-MM-yyyy` notation.
 *
 * The default `NativeDateAdapter` parses typed input with `new Date(value)`, which
 * interprets `10-06-2026` as 6 October (US month-first) regardless of the configured
 * locale. This adapter parses day-first so manual entry matches what the user sees.
 */
@Injectable()
export class DutchDateAdapter extends NativeDateAdapter {
  override parse(value: unknown): Date | null {
    if (typeof value === 'string' && value.trim()) {
      const match = value.trim().match(/^(\d{1,2})[-/.](\d{1,2})[-/.](\d{2,4})$/);
      if (match) {
        const day = +match[1];
        const month = +match[2] - 1;
        let year = +match[3];
        if (year < 100) year += 2000;
        const date = new Date(year, month, day);
        return isNaN(date.getTime()) ? null : date;
      }
      const timestamp = Date.parse(value);
      return isNaN(timestamp) ? null : new Date(timestamp);
    }
    return value ? new Date(value as string | number | Date) : null;
  }

  override format(date: Date, _displayFormat: unknown): string {
    const day = String(date.getDate()).padStart(2, '0');
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const year = date.getFullYear();
    return `${day}-${month}-${year}`;
  }
}

export const DUTCH_DATE_FORMATS: MatDateFormats = {
  parse: { dateInput: 'DD-MM-YYYY' },
  display: {
    dateInput: 'DD-MM-YYYY',
    monthYearLabel: 'MMM YYYY',
    dateA11yLabel: 'DD-MM-YYYY',
    monthYearA11yLabel: 'MMMM YYYY',
  },
};
