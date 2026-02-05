/**
 * Shared date/time utilities for consistent timezone handling.
 *
 * Principles:
 * - Backend stores DateTimeOffset in UTC
 * - Frontend displays dates/times in browser's local timezone
 * - API boundary uses ISO strings with explicit UTC offsets
 */

/**
 * Converts a Date to an ISO string with the local timezone offset.
 * Use this instead of .toISOString() when sending dates to the API.
 *
 * Example: For Feb 5, 2025 10:00 AM AEST (UTC+10):
 * - .toISOString() → "2025-02-05T00:00:00.000Z" (wrong - converts to UTC)
 * - toLocalISOString() → "2025-02-05T10:00:00+10:00" (correct - preserves local time)
 */
export function toLocalISOString(date: Date): string {
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const pad = (n: number) => String(Math.abs(n)).padStart(2, '0');
  const hours = pad(Math.floor(Math.abs(offset) / 60));
  const minutes = pad(Math.abs(offset) % 60);

  return (
    date.getFullYear() +
    '-' +
    pad(date.getMonth() + 1) +
    '-' +
    pad(date.getDate()) +
    'T' +
    pad(date.getHours()) +
    ':' +
    pad(date.getMinutes()) +
    ':' +
    pad(date.getSeconds()) +
    sign +
    hours +
    ':' +
    minutes
  );
}

/**
 * Gets a YYYY-MM-DD date key from a Date using local date components.
 * Use this for grouping meetings by date.
 *
 * Example: For Feb 5, 2025 10:00 AM in any timezone:
 * - Returns "2025-02-05" (local date, not UTC date)
 */
export function getLocalDateKey(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Formats time from an ISO date string in 12-hour format.
 * Returns "--:--" if the date string is null/invalid.
 *
 * Example: "2025-02-05T14:30:00Z" → "2:30" (in local timezone)
 */
export function formatTime(dateStr: string | null): string {
  if (!dateStr) return '--:--';
  const date = new Date(dateStr);
  const hours = date.getHours();
  const minutes = date.getMinutes();
  const displayHours = hours % 12 || 12;
  return `${displayHours}:${minutes.toString().padStart(2, '0')}`;
}

/**
 * Formats AM/PM from an ISO date string.
 * Returns empty string if the date string is null/invalid.
 *
 * Example: "2025-02-05T14:30:00Z" → "PM" (in local timezone)
 */
export function formatAmPm(dateStr: string | null): string {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  return date.getHours() >= 12 ? 'PM' : 'AM';
}

/**
 * Formats a full date and time using the browser's locale.
 *
 * Example: "2025-02-05T14:30:00Z" → "Feb 5 2:30 PM" (format varies by locale)
 */
export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  return (
    date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }) +
    ' ' +
    date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
  );
}

/**
 * Formats a date using the browser's locale.
 * Defaults to short month and numeric day (e.g., "Feb 5").
 *
 * Example: "2025-02-05T14:30:00Z" → "Feb 5" (format varies by locale)
 */
export function formatDateLabel(date: Date): string {
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}
