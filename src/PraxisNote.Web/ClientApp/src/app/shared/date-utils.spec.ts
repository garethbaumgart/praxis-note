import { describe, it, expect, vi, afterEach } from 'vitest';
import { formatTimeAgo } from './date-utils';

describe('formatTimeAgo', () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it('should return "Never" for null input', () => {
    expect(formatTimeAgo(null)).toBe('Never');
  });

  it('should return "Never" for invalid date string', () => {
    expect(formatTimeAgo('not-a-date')).toBe('Never');
  });

  it('should return "Never" for empty string', () => {
    expect(formatTimeAgo('')).toBe('Never');
  });

  it('should return "Just now" for timestamps less than 1 minute ago', () => {
    const now = new Date().toISOString();
    expect(formatTimeAgo(now)).toBe('Just now');
  });

  it('should return "1 min ago" for exactly 1 minute ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-15T10:05:00Z'));
    expect(formatTimeAgo('2026-01-15T10:04:00Z')).toBe('1 min ago');
  });

  it('should return "30 mins ago" for 30 minutes ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-15T10:30:00Z'));
    expect(formatTimeAgo('2026-01-15T10:00:00Z')).toBe('30 mins ago');
  });

  it('should return "1 hour ago" for exactly 1 hour ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-15T11:00:00Z'));
    expect(formatTimeAgo('2026-01-15T10:00:00Z')).toBe('1 hour ago');
  });

  it('should return "5 hours ago" for 5 hours ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-15T15:00:00Z'));
    expect(formatTimeAgo('2026-01-15T10:00:00Z')).toBe('5 hours ago');
  });

  it('should return "1 day ago" for exactly 1 day ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-16T10:00:00Z'));
    expect(formatTimeAgo('2026-01-15T10:00:00Z')).toBe('1 day ago');
  });

  it('should return "3 days ago" for 3 days ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-18T10:00:00Z'));
    expect(formatTimeAgo('2026-01-15T10:00:00Z')).toBe('3 days ago');
  });

  it('should return locale date string for 7+ days ago', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-01-25T10:00:00Z'));
    const result = formatTimeAgo('2026-01-15T10:00:00Z');
    // Should be a locale date string, not a relative time
    expect(result).not.toContain('ago');
    expect(result).not.toBe('Never');
    expect(result).not.toBe('Just now');
  });
});
