import { AddComponent } from './add.component';

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
