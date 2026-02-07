import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { GreetingService } from './greeting.service';

describe('GreetingService', () => {
  let service: GreetingService;
  let mockStorage: Record<string, string>;

  beforeEach(() => {
    service = new GreetingService();
    mockStorage = {};

    // Mock localStorage for node test environment
    const localStorageMock = {
      getItem: vi.fn((key: string) => mockStorage[key] ?? null),
      setItem: vi.fn((key: string, value: string) => { mockStorage[key] = value; }),
      removeItem: vi.fn((key: string) => { delete mockStorage[key]; }),
      clear: vi.fn(() => { mockStorage = {}; }),
      get length() { return Object.keys(mockStorage).length; },
      key: vi.fn((i: number) => Object.keys(mockStorage)[i] ?? null),
    };
    vi.stubGlobal('localStorage', localStorageMock);
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  // ── Basic functionality ──────────────────────────────────────

  it('should return a non-empty string', () => {
    const result = service.generateGreeting('Gareth');
    expect(result).toBeTruthy();
    expect(typeof result).toBe('string');
    expect(result.length).toBeGreaterThan(0);
  });

  it('should replace {name} placeholder with actual first name', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T09:00:00')); // Tuesday morning

    // Run many times to increase chance of getting a name-containing greeting
    const results = new Set<string>();
    for (let i = 0; i < 100; i++) {
      mockStorage = {}; // Reset to avoid repeat-prevention filtering
      results.add(service.generateGreeting('Gareth'));
    }

    // At least some greetings should contain the name
    const withName = [...results].filter(g => g.includes('Gareth'));
    expect(withName.length).toBeGreaterThan(0);

    // No greeting should contain the raw placeholder
    const withPlaceholder = [...results].filter(g => g.includes('{name}'));
    expect(withPlaceholder.length).toBe(0);
  });

  it('should work with greetings that have no {name} placeholder', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T09:00:00')); // Tuesday morning

    const results = new Set<string>();
    for (let i = 0; i < 100; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    // Some greetings should not contain the name (e.g. "What's on the agenda?", "Let's get things done")
    const withoutName = [...results].filter(g => !g.includes('Gareth'));
    expect(withoutName.length).toBeGreaterThan(0);
  });

  // ── Time-of-day greetings ────────────────────────────────────

  it('should return morning greetings when hour is between 5 and 11', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T09:00:00')); // Tuesday 9am

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const morningGreetings = ['Morning, Gareth', 'Rise and shine, Gareth', 'Top of the morning, Gareth'];
    const hasMorning = morningGreetings.some(g => results.has(g));
    expect(hasMorning).toBe(true);

    // Should NOT have afternoon/evening-specific greetings
    expect(results.has('Afternoon, Gareth')).toBe(false);
    expect(results.has('Good afternoon, Gareth')).toBe(false);
    expect(results.has('Evening, Gareth')).toBe(false);
    expect(results.has('Good evening, Gareth')).toBe(false);
    expect(results.has('Burning the midnight oil?')).toBe(false);
  });

  it('should return afternoon greetings when hour is between 12 and 17', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T14:00:00')); // Tuesday 2pm

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const afternoonGreetings = ['Afternoon, Gareth', 'Good afternoon, Gareth'];
    const hasAfternoon = afternoonGreetings.some(g => results.has(g));
    expect(hasAfternoon).toBe(true);

    // Should NOT have morning-specific greetings
    expect(results.has('Morning, Gareth')).toBe(false);
    expect(results.has('Rise and shine, Gareth')).toBe(false);
  });

  it('should return evening greetings when hour is between 18 and 21', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T20:00:00')); // Tuesday 8pm

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const eveningGreetings = ['Evening, Gareth', 'Good evening, Gareth'];
    const hasEvening = eveningGreetings.some(g => results.has(g));
    expect(hasEvening).toBe(true);

    // Should NOT have morning-specific greetings
    expect(results.has('Morning, Gareth')).toBe(false);
    expect(results.has('Burning the midnight oil?')).toBe(false);
  });

  it('should return late night greetings when hour is 22-23 or 0-4', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T23:00:00')); // Tuesday 11pm

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const lateNightGreetings = ['Burning the midnight oil?', 'Night owl mode, Gareth'];
    const hasLateNight = lateNightGreetings.some(g => results.has(g));
    expect(hasLateNight).toBe(true);

    // Should NOT have morning/afternoon/evening-specific greetings
    expect(results.has('Morning, Gareth')).toBe(false);
    expect(results.has('Afternoon, Gareth')).toBe(false);
    expect(results.has('Evening, Gareth')).toBe(false);
  });

  it('should return late night greetings at 2am', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T02:00:00')); // Tuesday 2am

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const lateNightGreetings = ['Burning the midnight oil?', 'Night owl mode, Gareth'];
    const hasLateNight = lateNightGreetings.some(g => results.has(g));
    expect(hasLateNight).toBe(true);
  });

  // ── Day-of-week greetings ────────────────────────────────────

  it('should return Friday greetings on day 5', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-06T10:00:00')); // Friday

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const fridayGreetings = ['Happy Friday, Gareth!', 'Friday vibes, Gareth'];
    const hasFriday = fridayGreetings.some(g => results.has(g));
    expect(hasFriday).toBe(true);
  });

  it('should NOT return Friday greetings on non-Friday days', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T10:00:00')); // Tuesday

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    expect(results.has('Happy Friday, Gareth!')).toBe(false);
    expect(results.has('Friday vibes, Gareth')).toBe(false);
  });

  it('should return Monday greetings on day 1', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-02T10:00:00')); // Monday

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    const mondayGreetings = ["Monday — let's do this, Gareth", 'New week, fresh start'];
    const hasMonday = mondayGreetings.some(g => results.has(g));
    expect(hasMonday).toBe(true);
  });

  it('should NOT return Monday greetings on non-Monday days', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-04T10:00:00')); // Wednesday

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    expect(results.has("Monday — let's do this, Gareth")).toBe(false);
    expect(results.has('New week, fresh start')).toBe(false);
  });

  it('should return Wednesday greetings on day 3', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-04T10:00:00')); // Wednesday

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage = {};
      results.add(service.generateGreeting('Gareth'));
    }

    expect(results.has('Hump day, Gareth!')).toBe(true);
  });

  // ── Session-gap greetings ────────────────────────────────────

  it('should return quickReturn greetings when last visit was less than 30 min ago', () => {
    vi.useFakeTimers();
    const now = new Date('2026-02-03T10:00:00');
    vi.setSystemTime(now);

    // Set last visit to 10 minutes ago
    const tenMinAgo = new Date(now.getTime() - 10 * 60000);
    mockStorage['praxisnote.greeting.lastVisit'] = tenMinAgo.toISOString();

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      // Re-set last visit since generateGreeting updates it
      mockStorage['praxisnote.greeting.lastVisit'] = tenMinAgo.toISOString();
      delete mockStorage['praxisnote.greeting.lastText'];
      results.add(service.generateGreeting('Gareth'));
    }

    const quickReturnGreetings = ['Back already?', 'Miss me?'];
    const hasQuickReturn = quickReturnGreetings.some(g => results.has(g));
    expect(hasQuickReturn).toBe(true);
  });

  it('should return longAbsence greetings when last visit was more than 3 days ago', () => {
    vi.useFakeTimers();
    const now = new Date('2026-02-03T10:00:00');
    vi.setSystemTime(now);

    // Set last visit to 5 days ago
    const fiveDaysAgo = new Date(now.getTime() - 5 * 86400000);
    mockStorage['praxisnote.greeting.lastVisit'] = fiveDaysAgo.toISOString();

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage['praxisnote.greeting.lastVisit'] = fiveDaysAgo.toISOString();
      delete mockStorage['praxisnote.greeting.lastText'];
      results.add(service.generateGreeting('Gareth'));
    }

    expect(results.has('Long time no see, Gareth')).toBe(true);
  });

  it('should return firstToday greetings when last visit was on a different calendar day', () => {
    vi.useFakeTimers();
    const now = new Date('2026-02-03T10:00:00');
    vi.setSystemTime(now);

    // Set last visit to yesterday (within 3 days but different calendar day)
    const yesterday = new Date('2026-02-02T22:00:00');
    mockStorage['praxisnote.greeting.lastVisit'] = yesterday.toISOString();

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage['praxisnote.greeting.lastVisit'] = yesterday.toISOString();
      delete mockStorage['praxisnote.greeting.lastText'];
      results.add(service.generateGreeting('Gareth'));
    }

    const firstTodayGreetings = ['Gareth returns!', 'Welcome back, Gareth'];
    const hasFirstToday = firstTodayGreetings.some(g => results.has(g));
    expect(hasFirstToday).toBe(true);
  });

  it('should return firstToday when there is no previous visit', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T10:00:00'));

    // No last visit in storage — counts as firstToday
    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      // Clear storage each time to simulate no history
      mockStorage = {};
      vi.stubGlobal('localStorage', {
        getItem: vi.fn((key: string) => mockStorage[key] ?? null),
        setItem: vi.fn((key: string, value: string) => { mockStorage[key] = value; }),
        removeItem: vi.fn(),
        clear: vi.fn(),
        length: 0,
        key: vi.fn(),
      });
      results.add(service.generateGreeting('Gareth'));
    }

    const firstTodayGreetings = ['Gareth returns!', 'Welcome back, Gareth'];
    const hasFirstToday = firstTodayGreetings.some(g => results.has(g));
    expect(hasFirstToday).toBe(true);
  });

  it('should return firstToday (not quickReturn) for cross-midnight visit under 30 min', () => {
    vi.useFakeTimers();
    // 12:05am — last visit was 11:55pm the previous day (10 min ago, different calendar day)
    const now = new Date('2026-02-04T00:05:00');
    vi.setSystemTime(now);

    const lastVisit = new Date('2026-02-03T23:55:00');
    mockStorage['praxisnote.greeting.lastVisit'] = lastVisit.toISOString();

    const results = new Set<string>();
    for (let i = 0; i < 200; i++) {
      mockStorage['praxisnote.greeting.lastVisit'] = lastVisit.toISOString();
      delete mockStorage['praxisnote.greeting.lastText'];
      results.add(service.generateGreeting('Gareth'));
    }

    // Should NOT get quickReturn greetings since it's a different calendar day
    expect(results.has('Back already?')).toBe(false);
    expect(results.has('Miss me?')).toBe(false);

    // Should get firstToday greetings
    const firstTodayGreetings = ['Gareth returns!', 'Welcome back, Gareth'];
    const hasFirstToday = firstTodayGreetings.some(g => results.has(g));
    expect(hasFirstToday).toBe(true);
  });

  // ── Repeat prevention ────────────────────────────────────────

  it('should not repeat the same greeting back-to-back', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T10:00:00')); // Tuesday morning

    const greetings: string[] = [];
    for (let i = 0; i < 50; i++) {
      greetings.push(service.generateGreeting('Gareth'));
    }

    for (let i = 1; i < greetings.length; i++) {
      // Consecutive greetings should differ (unless there's only one eligible)
      // With the broad set of "any time" + "morning" + "firstToday" on Tuesday, there should always be >1 eligible
      expect(greetings[i]).not.toBe(greetings[i - 1]);
    }
  });

  // ── localStorage resilience ──────────────────────────────────

  it('should not throw when localStorage is unavailable', () => {
    vi.stubGlobal('localStorage', {
      getItem: vi.fn(() => { throw new Error('localStorage disabled'); }),
      setItem: vi.fn(() => { throw new Error('localStorage disabled'); }),
      removeItem: vi.fn(() => { throw new Error('localStorage disabled'); }),
      clear: vi.fn(),
      length: 0,
      key: vi.fn(),
    });

    expect(() => service.generateGreeting('Gareth')).not.toThrow();
    const result = service.generateGreeting('Gareth');
    expect(result).toBeTruthy();
  });

  it('should handle invalid date in localStorage gracefully', () => {
    mockStorage['praxisnote.greeting.lastVisit'] = 'not-a-valid-date';

    expect(() => service.generateGreeting('Gareth')).not.toThrow();
    const result = service.generateGreeting('Gareth');
    expect(result).toBeTruthy();
  });

  // ── Greeting count ───────────────────────────────────────────

  it('should have at least 25 greeting variants', () => {
    // Use deterministic RNG to avoid flakiness from Math.random
    vi.useFakeTimers();

    let seed = 42;
    const randomMock = vi.spyOn(Math, 'random').mockImplementation(() => {
      seed = (seed * 1664525 + 1013904223) % 0x100000000;
      return seed / 0x100000000;
    });

    try {
      const allGreetings = new Set<string>();

      // Morning on Monday (firstToday)
      vi.setSystemTime(new Date('2026-02-02T09:00:00'));
      for (let i = 0; i < 100; i++) {
        mockStorage = {};
        allGreetings.add(service.generateGreeting('Test'));
      }

      // Afternoon on Friday (firstToday)
      vi.setSystemTime(new Date('2026-02-06T14:00:00'));
      for (let i = 0; i < 100; i++) {
        mockStorage = {};
        allGreetings.add(service.generateGreeting('Test'));
      }

      // Evening on Wednesday (firstToday)
      vi.setSystemTime(new Date('2026-02-04T20:00:00'));
      for (let i = 0; i < 100; i++) {
        mockStorage = {};
        allGreetings.add(service.generateGreeting('Test'));
      }

      // Late night on Tuesday (quickReturn — same day)
      vi.setSystemTime(new Date('2026-02-03T23:00:00'));
      const recent = new Date('2026-02-03T22:55:00');
      for (let i = 0; i < 100; i++) {
        mockStorage = { 'praxisnote.greeting.lastVisit': recent.toISOString() };
        allGreetings.add(service.generateGreeting('Test'));
      }

      // Long absence
      vi.setSystemTime(new Date('2026-02-03T10:00:00'));
      const longAgo = new Date('2026-01-25T10:00:00');
      for (let i = 0; i < 100; i++) {
        mockStorage = { 'praxisnote.greeting.lastVisit': longAgo.toISOString() };
        allGreetings.add(service.generateGreeting('Test'));
      }

      expect(allGreetings.size).toBeGreaterThanOrEqual(25);
    } finally {
      randomMock.mockRestore();
    }
  });

  // ── Records visit ────────────────────────────────────────────

  it('should record the visit timestamp in localStorage', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-02-03T10:00:00'));

    service.generateGreeting('Gareth');

    expect(mockStorage['praxisnote.greeting.lastVisit']).toBeDefined();
    const stored = new Date(mockStorage['praxisnote.greeting.lastVisit']);
    expect(stored.getTime()).toBe(new Date('2026-02-03T10:00:00').getTime());
  });

  it('should save the last greeting template in localStorage', () => {
    service.generateGreeting('Gareth');

    expect(mockStorage['praxisnote.greeting.lastText']).toBeDefined();
    expect(typeof mockStorage['praxisnote.greeting.lastText']).toBe('string');
    expect(mockStorage['praxisnote.greeting.lastText'].length).toBeGreaterThan(0);
  });
});
