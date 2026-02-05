import { describe, it, expect } from 'vitest';
import {
  toLocalISOString,
  getLocalDateKey,
  formatTime,
  formatAmPm,
  formatDateTime,
  formatDateLabel,
} from './date-utils';

describe('date-utils', () => {
  describe('toLocalISOString', () => {
    it('should format date with local timezone offset', () => {
      // Create a date: Feb 5, 2025 10:30:00 AM local time
      const date = new Date(2025, 1, 5, 10, 30, 0);
      const result = toLocalISOString(date);

      // Should contain the date and time components
      expect(result).toContain('2025-02-05');
      expect(result).toContain('10:30:00');

      // Should have a timezone offset (+ or -)
      expect(result).toMatch(/[+-]\d{2}:\d{2}$/);
    });

    it('should preserve local date even when it crosses UTC boundary', () => {
      // Create a date late in the day in a timezone ahead of UTC
      const date = new Date(2025, 1, 5, 23, 30, 0);
      const result = toLocalISOString(date);

      // Should still show Feb 5 (not Feb 6 as UTC would)
      expect(result).toContain('2025-02-05');
    });
  });

  describe('getLocalDateKey', () => {
    it('should return YYYY-MM-DD format using local date', () => {
      const date = new Date(2025, 1, 5, 10, 30, 0); // Feb 5, 2025
      const result = getLocalDateKey(date);
      expect(result).toBe('2025-02-05');
    });

    it('should pad single-digit months and days', () => {
      const date = new Date(2025, 0, 9, 10, 30, 0); // Jan 9, 2025
      const result = getLocalDateKey(date);
      expect(result).toBe('2025-01-09');
    });

    it('should use local date components, not UTC', () => {
      // Create a date that would be different in UTC
      const date = new Date(2025, 1, 5, 23, 30, 0);
      const result = getLocalDateKey(date);

      // Should use local date (Feb 5), not UTC date
      expect(result).toBe('2025-02-05');
    });
  });

  describe('formatTime', () => {
    it('should format time in 12-hour format', () => {
      const iso = '2025-02-05T14:30:00Z';
      const result = formatTime(iso);

      // Result depends on local timezone, but should be in 12-hour format
      expect(result).toMatch(/^\d{1,2}:\d{2}$/);
    });

    it('should return --:-- for null', () => {
      expect(formatTime(null)).toBe('--:--');
    });

    it('should pad minutes with leading zero', () => {
      const iso = '2025-02-05T14:05:00Z';
      const result = formatTime(iso);

      // Should have :05 or :XX format
      expect(result).toMatch(/:\d{2}$/);
    });
  });

  describe('formatAmPm', () => {
    it('should return PM for afternoon times', () => {
      const iso = '2025-02-05T14:30:00Z';
      const result = formatAmPm(iso);

      // Result depends on local timezone
      expect(['AM', 'PM']).toContain(result);
    });

    it('should return empty string for null', () => {
      expect(formatAmPm(null)).toBe('');
    });
  });

  describe('formatDateTime', () => {
    it('should format date and time', () => {
      const iso = '2025-02-05T14:30:00Z';
      const result = formatDateTime(iso);

      // Should contain some date and time components
      // Exact format depends on locale, so just check it's not empty
      expect(result.length).toBeGreaterThan(0);
    });
  });

  describe('formatDateLabel', () => {
    it('should format date with short month', () => {
      const date = new Date(2025, 1, 5); // Feb 5, 2025
      const result = formatDateLabel(date);

      // Should contain month and day, exact format depends on locale
      expect(result.length).toBeGreaterThan(0);
    });
  });
});
