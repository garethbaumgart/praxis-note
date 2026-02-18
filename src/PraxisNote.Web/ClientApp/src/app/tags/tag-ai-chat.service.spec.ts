import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { MessageService } from 'primeng/api';
import { TagAiChatService } from './tag-ai-chat.service';

function mockFetchResponse(data: unknown, ok = true): ReturnType<typeof vi.fn> {
  return vi.fn().mockResolvedValue({
    ok,
    json: () => Promise.resolve(data),
  } as Partial<Response>);
}

describe('TagAiChatService', () => {
  let service: TagAiChatService;
  let fetchSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    fetchSpy = mockFetchResponse({ starters: [] });
    vi.stubGlobal('fetch', fetchSpy);

    TestBed.configureTestingModule({
      providers: [MessageService],
    });

    service = TestBed.inject(TagAiChatService);
  });

  afterEach(() => {
    if (service) service.close();
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('should start in idle state', () => {
    expect(service.state()).toBe('idle');
  });

  it('should not be open initially', () => {
    expect(service.isOpen()).toBe(false);
  });

  it('should not be collapsed initially', () => {
    expect(service.isCollapsed()).toBe(false);
  });

  it('should have no messages initially', () => {
    expect(service.messages()).toEqual([]);
  });

  it('should have no starters initially', () => {
    expect(service.starters()).toEqual([]);
  });

  it('should have no error initially', () => {
    expect(service.error()).toBeNull();
  });

  it('should have hasMessages as false initially', () => {
    expect(service.hasMessages()).toBe(false);
  });

  it('open should set isOpen to true', () => {
    service.open('tag-1');
    expect(service.isOpen()).toBe(true);
  });

  it('open should transition to ready state after loading starters', async () => {
    fetchSpy = mockFetchResponse({ starters: ['q1?'] });
    vi.stubGlobal('fetch', fetchSpy);
    service.open('tag-1');
    await vi.waitFor(() => expect(service.state()).toBe('ready'));
  });

  it('open should clear previous messages', () => {
    service.open('tag-1');
    expect(service.messages()).toEqual([]);
  });

  it('open should load starters from API', async () => {
    const mockStarters = ['Question 1?', 'Question 2?'];
    fetchSpy = mockFetchResponse({ starters: mockStarters });
    vi.stubGlobal('fetch', fetchSpy);
    service.open('tag-1');
    await vi.waitFor(() => expect(service.starters()).toEqual(mockStarters));
  });

  it('open should set empty starters on API error', async () => {
    fetchSpy = mockFetchResponse({}, false);
    vi.stubGlobal('fetch', fetchSpy);
    service.open('tag-1');
    await vi.waitFor(() => {
      expect(service.starters()).toEqual([]);
      expect(service.state()).toBe('ready');
    });
  });

  it('open should set empty starters on fetch rejection', async () => {
    fetchSpy = vi.fn().mockRejectedValue(new Error('Network error'));
    vi.stubGlobal('fetch', fetchSpy);
    service.open('tag-1');
    await vi.waitFor(() => {
      expect(service.starters()).toEqual([]);
      expect(service.state()).toBe('ready');
    });
  });

  it('open should expand if already open for same tag', async () => {
    service.open('tag-1');
    await vi.waitFor(() => expect(service.state()).toBe('ready'));
    service.collapse();
    expect(service.isCollapsed()).toBe(true);
    service.open('tag-1');
    expect(service.isCollapsed()).toBe(false);
  });

  it('close should set isOpen to false', () => {
    service.open('tag-1');
    service.close();
    expect(service.isOpen()).toBe(false);
  });

  it('close should reset state to idle', () => {
    service.open('tag-1');
    service.close();
    expect(service.state()).toBe('idle');
  });

  it('close should clear messages', () => {
    service.open('tag-1');
    service.close();
    expect(service.messages()).toEqual([]);
  });

  it('close should clear error', () => {
    service.open('tag-1');
    service.close();
    expect(service.error()).toBeNull();
  });

  it('collapse/expand should toggle collapsed state', () => {
    service.collapse();
    expect(service.isCollapsed()).toBe(true);
    service.expand();
    expect(service.isCollapsed()).toBe(false);
  });

  it('clearChat should clear messages and reload starters', async () => {
    service.open('tag-1');
    await vi.waitFor(() => expect(service.state()).toBe('ready'));
    service.clearChat();
    expect(service.messages()).toEqual([]);
    await vi.waitFor(() => expect(fetchSpy).toHaveBeenCalledTimes(2));
  });

  it('stop should not throw when no stream is active', () => {
    expect(() => service.stop()).not.toThrow();
  });
});
