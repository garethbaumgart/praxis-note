/**
 * Shared date utilities for timezone-safe date operations.
 *
 * Principle: Store UTC, Display Local.
 * - Backend stores DateTimeOffset in UTC
 * - Frontend always converts to local timezone for display and grouping
 * - API boundary: send dates with explicit UTC offsets, never bare local times
 */

/**
 * Formats a Date as an ISO string with the local timezone offset,
 * preserving the user's intended local date and time.
 *
 * Example (AEST UTC+10):
 *   toLocalISOString(new Date(2025, 1, 5, 2, 0)) => "2025-02-05T02:00:00+10:00"
 *
 * Compare with .toISOString() which would produce "2025-02-04T16:00:00.000Z"
 * (shifting the date to Feb 4 in UTC).
 */
export function toLocalISOString(date: Date): string {
  const offset = -date.getTimezoneOffset();
  const sign = offset >= 0 ? '+' : '-';
  const pad = (n: number) => String(Math.abs(n)).padStart(2, '0');
  const hours = pad(Math.floor(Math.abs(offset) / 60));
  const minutes = pad(Math.abs(offset) % 60);

  return (
    date.getFullYear() +
    '-' + pad(date.getMonth() + 1) +
    '-' + pad(date.getDate()) +
    'T' + pad(date.getHours()) +
    ':' + pad(date.getMinutes()) +
    ':' + pad(date.getSeconds()) +
    sign + hours + ':' + minutes
  );
}

/**
 * Extracts a YYYY-MM-DD date key using local date components.
 * This avoids the bug where setHours(0,0,0,0) + toISOString() shifts dates
 * across day boundaries for non-UTC timezones.
 */
export function getLocalDateKey(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Formats a time string like "10:30" from an ISO date string, using local timezone.
 * Returns hours in 12-hour format without AM/PM suffix.
 */
export function formatTime(dateStr: string | null): string {
  if (!dateStr) return '--:--';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '--:--';
  const hours = date.getHours() % 12 || 12;
  const minutes = date.getMinutes().toString().padStart(2, '0');
  return `${hours}:${minutes}`;
}

/**
 * Returns "AM" or "PM" for the given ISO date string, using local timezone.
 */
export function formatAmPm(dateStr: string | null): string {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  if (isNaN(date.getTime())) return '';
  return date.getHours() >= 12 ? 'PM' : 'AM';
}

/**
 * Formats a date+time for display, using the browser's locale.
 * Example: "Feb 5 10:00 AM"
 */
export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (isNaN(date.getTime())) return '';
  return (
    date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }) +
    ' ' +
    date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' })
  );
}

/**
 * Formats just the time portion for display, using the browser's locale.
 * Example: "10:00 AM"
 */
export function formatLocaleTime(iso: string): string {
  const date = new Date(iso);
  if (isNaN(date.getTime())) return '';
  return date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
}

/**
 * Formats a short date label using the browser's locale (not hardcoded en-US).
 * Example: "Feb 5"
 */
export function formatShortDate(date: Date): string {
  if (isNaN(date.getTime())) return '';
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

/**
 * Formats a relative time string from an ISO date, suitable for tooltip display.
 * Examples: "Just now", "5 mins ago", "2 hours ago", "3 days ago", "Feb 5"
 */
export function formatTimeAgo(isoDate: string | null): string {
  if (!isoDate) return 'Never';
  const ts = new Date(isoDate).getTime();
  if (isNaN(ts)) return 'Never';
  const diff = Date.now() - ts;
  const mins = Math.floor(diff / 60000);
  if (mins < 1) return 'Just now';
  if (mins < 60) return `${mins} min${mins > 1 ? 's' : ''} ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours} hour${hours > 1 ? 's' : ''} ago`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days} day${days > 1 ? 's' : ''} ago`;
  return new Date(isoDate).toLocaleDateString();
}
