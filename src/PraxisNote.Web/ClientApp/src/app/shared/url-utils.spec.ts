import { describe, it, expect } from 'vitest';
import { normalizeUrl, normalizeLinkUrl, normalizeImageUrl } from './url-utils';

describe('normalizeUrl', () => {
  // ── Basic normalization ──────────────────────────────────────

  it('should return null for empty string', () => {
    expect(normalizeUrl('')).toBeNull();
  });

  it('should return null for whitespace-only string', () => {
    expect(normalizeUrl('   ')).toBeNull();
  });

  it('should trim whitespace from input', () => {
    expect(normalizeUrl('  https://example.com  ')).toBe('https://example.com/');
  });

  it('should auto-prepend https:// when no protocol is present', () => {
    expect(normalizeUrl('example.com')).toBe('https://example.com/');
  });

  it('should preserve http:// protocol', () => {
    expect(normalizeUrl('http://example.com')).toBe('http://example.com/');
  });

  it('should preserve https:// protocol', () => {
    expect(normalizeUrl('https://example.com')).toBe('https://example.com/');
  });

  it('should preserve paths and query strings', () => {
    expect(normalizeUrl('https://example.com/path?q=1')).toBe('https://example.com/path?q=1');
  });

  it('should preserve hash fragments', () => {
    expect(normalizeUrl('https://example.com/page#section')).toBe(
      'https://example.com/page#section',
    );
  });

  // ── Protocol filtering ──────────────────────────────────────

  it('should allow mailto: protocol by default', () => {
    expect(normalizeUrl('mailto:user@example.com')).toBe('mailto:user@example.com');
  });

  it('should reject javascript: protocol', () => {
    expect(normalizeUrl('javascript:alert(1)')).toBeNull();
  });

  it('should reject data: protocol', () => {
    expect(normalizeUrl('data:text/html,<h1>Hi</h1>')).toBeNull();
  });

  it('should reject ftp: protocol by default', () => {
    expect(normalizeUrl('ftp://example.com')).toBeNull();
  });

  it('should allow custom protocols via allowedProtocols parameter', () => {
    expect(normalizeUrl('ftp://example.com', ['ftp:'])).toBe('ftp://example.com/');
  });

  // ── Invalid URLs ──────────────────────────────────────────

  it('should return null for completely invalid URL', () => {
    expect(normalizeUrl('not a valid url at all %%%')).toBeNull();
  });
});

describe('normalizeLinkUrl', () => {
  it('should allow http', () => {
    expect(normalizeLinkUrl('http://example.com')).toBe('http://example.com/');
  });

  it('should allow https', () => {
    expect(normalizeLinkUrl('https://example.com')).toBe('https://example.com/');
  });

  it('should allow mailto', () => {
    expect(normalizeLinkUrl('mailto:user@example.com')).toBe('mailto:user@example.com');
  });

  it('should reject javascript', () => {
    expect(normalizeLinkUrl('javascript:alert(1)')).toBeNull();
  });

  it('should auto-prepend https for bare domains', () => {
    expect(normalizeLinkUrl('example.com')).toBe('https://example.com/');
  });
});

describe('normalizeImageUrl', () => {
  it('should allow http', () => {
    expect(normalizeImageUrl('http://example.com/img.png')).toBe(
      'http://example.com/img.png',
    );
  });

  it('should allow https', () => {
    expect(normalizeImageUrl('https://example.com/img.png')).toBe(
      'https://example.com/img.png',
    );
  });

  it('should reject mailto (not valid for images)', () => {
    expect(normalizeImageUrl('mailto:user@example.com')).toBeNull();
  });

  it('should reject javascript', () => {
    expect(normalizeImageUrl('javascript:alert(1)')).toBeNull();
  });

  it('should auto-prepend https for bare domains', () => {
    expect(normalizeImageUrl('example.com/photo.jpg')).toBe(
      'https://example.com/photo.jpg',
    );
  });

  it('should return null for empty string', () => {
    expect(normalizeImageUrl('')).toBeNull();
  });
});
