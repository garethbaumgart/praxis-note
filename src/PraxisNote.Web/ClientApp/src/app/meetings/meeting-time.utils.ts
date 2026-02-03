/** Generate all 48 half-hour time labels: "12:00 AM", "12:30 AM", ..., "11:30 PM" */
function generateTimeOptions(): string[] {
  const options: string[] = [];
  for (let h = 0; h < 24; h++) {
    for (const m of [0, 30]) {
      const period = h < 12 ? 'AM' : 'PM';
      const hour12 = h === 0 ? 12 : h > 12 ? h - 12 : h;
      const min = m === 0 ? '00' : '30';
      options.push(`${hour12}:${min} ${period}`);
    }
  }
  return options;
}

/** Parse flexible time input into 24-hour { hours, minutes } or null if invalid.
 *
 * Supported formats:
 * - "6:30 PM", "6:30PM", "6:30p" (12-hour with colon)
 * - "18:30" (24-hour)
 * - "630pm", "630p", "1230a" (compact)
 * - "6p", "6pm", "6 PM", "12a" (hour-only)
 */
export function parseTimeInput(input: string): { hours: number; minutes: number } | null {
  if (!input || !input.trim()) return null;
  const raw = input.trim();

  // Try 24-hour format: "18:30", "9:00"
  const match24 = raw.match(/^(\d{1,2}):(\d{2})$/);
  if (match24) {
    const h = parseInt(match24[1], 10);
    const m = parseInt(match24[2], 10);
    if (h >= 0 && h <= 23 && m >= 0 && m <= 59) return { hours: h, minutes: m };
  }

  // Try 12-hour format with colon: "6:30 PM", "6:30PM", "6:30p", "6:30 p"
  const match12 = raw.match(/^(\d{1,2}):(\d{2})\s*([aApP][mM]?)$/);
  if (match12) {
    let h = parseInt(match12[1], 10);
    const m = parseInt(match12[2], 10);
    const p = match12[3].toLowerCase();
    if (h >= 1 && h <= 12 && m >= 0 && m <= 59) {
      const isPM = p.startsWith('p');
      if (isPM && h !== 12) h += 12;
      if (!isPM && h === 12) h = 0;
      return { hours: h, minutes: m };
    }
  }

  // Try compact format: "630pm", "630p", "630PM", "1230a"
  const matchCompact = raw.match(/^(\d{3,4})\s*([aApP][mM]?)$/);
  if (matchCompact) {
    const num = matchCompact[1];
    const p = matchCompact[2].toLowerCase();
    let h: number;
    let m: number;
    if (num.length === 3) {
      h = parseInt(num[0], 10);
      m = parseInt(num.substring(1), 10);
    } else {
      h = parseInt(num.substring(0, 2), 10);
      m = parseInt(num.substring(2), 10);
    }
    if (h >= 1 && h <= 12 && m >= 0 && m <= 59) {
      const isPM = p.startsWith('p');
      if (isPM && h !== 12) h += 12;
      if (!isPM && h === 12) h = 0;
      return { hours: h, minutes: m };
    }
  }

  // Try hour-only with period: "6p", "6pm", "6 PM", "12a"
  const matchHourOnly = raw.match(/^(\d{1,2})\s*([aApP][mM]?)$/);
  if (matchHourOnly) {
    let h = parseInt(matchHourOnly[1], 10);
    const p = matchHourOnly[2].toLowerCase();
    if (h >= 1 && h <= 12) {
      const isPM = p.startsWith('p');
      if (isPM && h !== 12) h += 12;
      if (!isPM && h === 12) h = 0;
      return { hours: h, minutes: 0 };
    }
  }

  return null;
}

/** Format 24-hour time as "6:30 PM" label */
export function formatTimeLabel(hours: number, minutes: number): string {
  const period = hours < 12 ? 'AM' : 'PM';
  const hour12 = hours === 0 ? 12 : hours > 12 ? hours - 12 : hours;
  const min = minutes < 10 ? '0' + minutes : '' + minutes;
  return `${hour12}:${min} ${period}`;
}

/** Get the nearest 30-min rounded time from the current clock */
export function getDefaultMeetingTime(): { hours: number; minutes: number } {
  const now = new Date();
  const m = now.getMinutes();
  let hours = now.getHours();
  let minutes: number;

  if (m < 15) {
    minutes = 0;
  } else if (m < 45) {
    minutes = 30;
  } else {
    hours = (hours + 1) % 24;
    minutes = 0;
  }

  return { hours, minutes };
}

/** All 48 half-hour time option labels */
export const ALL_TIME_OPTIONS = generateTimeOptions();
