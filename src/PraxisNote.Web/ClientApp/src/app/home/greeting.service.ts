import { Injectable } from '@angular/core';

export type TimeOfDay = 'morning' | 'afternoon' | 'evening' | 'lateNight';
export type SessionGap = 'firstToday' | 'quickReturn' | 'sameSession' | 'longAbsence';

export interface GreetingContext {
  timeOfDay: TimeOfDay;
  dayOfWeek: number; // 0=Sun, 6=Sat
  sessionGap: SessionGap;
}

interface GreetingTemplate {
  text: string;              // Use {name} as placeholder
  when?: (ctx: GreetingContext) => boolean; // Optional filter
}

const GREETINGS: GreetingTemplate[] = [
  // Time-of-day
  { text: 'Morning, {name}', when: ctx => ctx.timeOfDay === 'morning' },
  { text: 'Rise and shine, {name}', when: ctx => ctx.timeOfDay === 'morning' },
  { text: 'Top of the morning, {name}', when: ctx => ctx.timeOfDay === 'morning' },
  { text: 'Afternoon, {name}', when: ctx => ctx.timeOfDay === 'afternoon' },
  { text: 'Good afternoon, {name}', when: ctx => ctx.timeOfDay === 'afternoon' },
  { text: 'Evening, {name}', when: ctx => ctx.timeOfDay === 'evening' },
  { text: 'Good evening, {name}', when: ctx => ctx.timeOfDay === 'evening' },
  { text: 'Burning the midnight oil?', when: ctx => ctx.timeOfDay === 'lateNight' },
  { text: 'Night owl mode, {name}', when: ctx => ctx.timeOfDay === 'lateNight' },

  // Day-of-week
  { text: 'Happy Friday, {name}!', when: ctx => ctx.dayOfWeek === 5 },
  { text: 'Friday vibes, {name}', when: ctx => ctx.dayOfWeek === 5 },
  { text: "Monday — let's do this, {name}", when: ctx => ctx.dayOfWeek === 1 },
  { text: 'New week, fresh start', when: ctx => ctx.dayOfWeek === 1 },
  { text: 'Hump day, {name}!', when: ctx => ctx.dayOfWeek === 3 },

  // Session-gap
  { text: '{name} returns!', when: ctx => ctx.sessionGap === 'firstToday' || ctx.sessionGap === 'longAbsence' },
  { text: 'Welcome back, {name}', when: ctx => ctx.sessionGap === 'firstToday' || ctx.sessionGap === 'longAbsence' },
  { text: 'Long time no see, {name}', when: ctx => ctx.sessionGap === 'longAbsence' },
  { text: 'Back already?', when: ctx => ctx.sessionGap === 'quickReturn' },
  { text: 'Miss me?', when: ctx => ctx.sessionGap === 'quickReturn' },

  // Any time (always eligible)
  { text: 'Hey there, {name}' },
  { text: 'Good to see you, {name}' },
  { text: "What's on the agenda?" },
  { text: 'Ready when you are, {name}' },
  { text: "Let's get things done" },
  { text: 'Ready to roll, {name}?' },
];

@Injectable({ providedIn: 'root' })
export class GreetingService {
  private readonly LAST_VISIT_KEY = 'praxisnote.greeting.lastVisit';
  private readonly LAST_GREETING_KEY = 'praxisnote.greeting.lastText';

  generateGreeting(firstName: string): string {
    const ctx = this.buildContext();
    this.recordVisit();

    const eligible = GREETINGS.filter(g => !g.when || g.when(ctx));
    const lastGreeting = this.getLastGreeting();

    const filtered = eligible.length > 1
      ? eligible.filter(g => g.text !== lastGreeting)
      : eligible;

    const template = filtered[Math.floor(Math.random() * filtered.length)];
    const result = template.text.replace(/\{name\}/g, firstName);

    this.saveLastGreeting(template.text);
    return result;
  }

  private buildContext(): GreetingContext {
    const now = new Date();
    return {
      timeOfDay: this.getTimeOfDay(now.getHours()),
      dayOfWeek: now.getDay(),
      sessionGap: this.getSessionGap(now),
    };
  }

  private getTimeOfDay(hour: number): TimeOfDay {
    if (hour < 5) return 'lateNight';
    if (hour < 12) return 'morning';
    if (hour < 18) return 'afternoon';
    if (hour < 22) return 'evening';
    return 'lateNight';
  }

  private getSessionGap(now: Date): SessionGap {
    const lastVisit = this.getLastVisit();
    if (!lastVisit) return 'firstToday';

    const diffMs = now.getTime() - lastVisit.getTime();
    const diffMin = diffMs / 60000;
    const diffDays = diffMs / 86400000;

    const sameDay = now.toDateString() === lastVisit.toDateString();

    if (sameDay && diffMin < 30) return 'quickReturn';
    if (diffDays > 3) return 'longAbsence';
    if (!sameDay) return 'firstToday';

    return 'sameSession';
  }

  private getLastVisit(): Date | null {
    try {
      const stored = localStorage.getItem(this.LAST_VISIT_KEY);
      if (!stored) return null;
      const date = new Date(stored);
      return isNaN(date.getTime()) ? null : date;
    } catch { return null; }
  }

  private recordVisit(): void {
    try {
      localStorage.setItem(this.LAST_VISIT_KEY, new Date().toISOString());
    } catch { /* privacy-restricted */ }
  }

  private getLastGreeting(): string | null {
    try {
      return localStorage.getItem(this.LAST_GREETING_KEY);
    } catch { return null; }
  }

  private saveLastGreeting(template: string): void {
    try {
      localStorage.setItem(this.LAST_GREETING_KEY, template);
    } catch { /* privacy-restricted */ }
  }
}
