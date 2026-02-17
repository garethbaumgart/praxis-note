import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient, HttpClient } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { TagAiChatService } from './tag-ai-chat.service';
import { of, throwError } from 'rxjs';

describe('TagAiChatService', () => {
  let service: TagAiChatService;
  let httpSpy: { post: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    httpSpy = { post: vi.fn().mockReturnValue(of({ starters: [] })) };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        { provide: HttpClient, useValue: httpSpy },
        MessageService,
      ],
    });

    service = TestBed.inject(TagAiChatService);
  });

  afterEach(() => {
    if (service) service.close();
    vi.restoreAllMocks();
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

  it('open should transition to ready state after loading starters', () => {
    httpSpy.post.mockReturnValue(of({ starters: ['q1?'] }));
    service.open('tag-1');
    expect(service.state()).toBe('ready');
  });

  it('open should clear previous messages', () => {
    service.open('tag-1');
    expect(service.messages()).toEqual([]);
  });

  it('open should load starters from API', () => {
    const mockStarters = ['Question 1?', 'Question 2?'];
    httpSpy.post.mockReturnValue(of({ starters: mockStarters }));
    service.open('tag-1');
    expect(service.starters()).toEqual(mockStarters);
  });

  it('open should set empty starters on API error', () => {
    httpSpy.post.mockReturnValue(throwError(() => new Error('API error')));
    service.open('tag-1');
    expect(service.starters()).toEqual([]);
    expect(service.state()).toBe('ready');
  });

  it('open should expand if already open for same tag', () => {
    service.open('tag-1');
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

  it('clearChat should clear messages and reload starters', () => {
    service.open('tag-1');
    service.clearChat();
    expect(service.messages()).toEqual([]);
    expect(httpSpy.post).toHaveBeenCalledTimes(2);
  });

  it('stop should not throw when no stream is active', () => {
    expect(() => service.stop()).not.toThrow();
  });
});
