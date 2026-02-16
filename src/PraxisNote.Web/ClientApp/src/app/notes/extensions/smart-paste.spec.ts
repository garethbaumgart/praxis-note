import { describe, it, expect } from 'vitest';
import { parseStructuredText } from './smart-paste.extension';

describe('parseStructuredText', () => {
  // ── Bullet Lists ──────────────────────────────────────────

  describe('Bullet Lists', () => {
    it('converts bullet-dot lines into a <ul>', () => {
      const input = '• First item\n• Second item\n• Third item';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<ul><li><p>First item</p></li><li><p>Second item</p></li><li><p>Third item</p></li></ul>',
      );
    });

    it('converts dash-prefixed lines into a <ul>', () => {
      const input = '- Alpha\n- Beta\n- Gamma';
      const result = parseStructuredText(input);
      expect(result).toBe('<ul><li><p>Alpha</p></li><li><p>Beta</p></li><li><p>Gamma</p></li></ul>');
    });

    it('converts asterisk-prefixed lines into a <ul>', () => {
      const input = '* One\n* Two';
      const result = parseStructuredText(input);
      expect(result).toBe('<ul><li><p>One</p></li><li><p>Two</p></li></ul>');
    });

    it('handles various Unicode bullet markers', () => {
      const input = '◦ Circle\n▪ Square\n► Arrow\n· Middle dot\n‣ Triangular';
      const result = parseStructuredText(input);
      expect(result).toContain('<ul>');
      expect(result).toContain('<li><p>Circle</p></li>');
      expect(result).toContain('<li><p>Square</p></li>');
      expect(result).toContain('<li><p>Arrow</p></li>');
      expect(result).toContain('<li><p>Middle dot</p></li>');
      expect(result).toContain('<li><p>Triangular</p></li>');
    });

    it('handles indented bullet lines', () => {
      const input = '  - Indented one\n  - Indented two';
      const result = parseStructuredText(input);
      expect(result).toBe('<ul><li><p>Indented one</p></li><li><p>Indented two</p></li></ul>');
    });
  });

  // ── Numbered Lists ────────────────────────────────────────

  describe('Numbered Lists', () => {
    it('converts digit-dot lines into an <ol>', () => {
      const input = '1. First\n2. Second\n3. Third';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<ol><li><p>First</p></li><li><p>Second</p></li><li><p>Third</p></li></ol>',
      );
    });

    it('converts digit-paren lines into an <ol>', () => {
      const input = '1) First\n2) Second';
      const result = parseStructuredText(input);
      expect(result).toBe('<ol><li><p>First</p></li><li><p>Second</p></li></ol>');
    });

    it('converts letter-prefixed lines into an <ol>', () => {
      const input = 'a. Apple\nb. Banana\nc. Cherry';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<ol><li><p>Apple</p></li><li><p>Banana</p></li><li><p>Cherry</p></li></ol>',
      );
    });

    it('converts roman numeral lines into an <ol>', () => {
      const input = 'i. First\nii. Second\niii. Third';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<ol><li><p>First</p></li><li><p>Second</p></li><li><p>Third</p></li></ol>',
      );
    });
  });

  // ── ALL CAPS Headings ─────────────────────────────────────

  describe('ALL CAPS Headings', () => {
    it('converts ALL CAPS line followed by blank line into <h2>', () => {
      const input = 'INTRODUCTION\n\nSome text here.';
      const result = parseStructuredText(input);
      expect(result).toBe('<h2>INTRODUCTION</h2><p>Some text here.</p>');
    });

    it('converts heading with numbers and special chars', () => {
      const input = 'SECTION 2: KEY FINDINGS\n\nDetails below.';
      const result = parseStructuredText(input);
      expect(result).toBe('<h2>SECTION 2: KEY FINDINGS</h2><p>Details below.</p>');
    });

    it('does not treat short ALL CAPS as heading (less than 3 chars after first)', () => {
      const input = 'AB\n\nSome text.';
      const result = parseStructuredText(input);
      // "AB" is only 2 chars total; regex requires at least 3 total (1 initial + 2-78 more characters)
      expect(result).not.toContain('<h2>');
    });

    it('does not treat ALL CAPS without trailing blank line as heading', () => {
      const input = 'INTRODUCTION\nThis is the intro text.';
      const result = parseStructuredText(input);
      // Should be treated as a paragraph since no blank line follows
      expect(result).not.toContain('<h2>');
      expect(result).toContain('<p>');
    });
  });

  // ── Plain Paragraphs ──────────────────────────────────────

  describe('Plain Paragraphs', () => {
    it('joins consecutive plain lines into a single <p>', () => {
      const input = 'This is line one.\nThis is line two.\nThis is line three.';
      const result = parseStructuredText(input);
      expect(result).toBe('<p>This is line one. This is line two. This is line three.</p>');
    });

    it('separates paragraphs at blank lines', () => {
      const input = 'First paragraph.\n\nSecond paragraph.';
      const result = parseStructuredText(input);
      expect(result).toBe('<p>First paragraph.</p><p>Second paragraph.</p>');
    });
  });

  // ── Mixed Content ─────────────────────────────────────────

  describe('Mixed Content', () => {
    it('handles heading + paragraph + bullet list', () => {
      const input = 'OVERVIEW\n\nThis is the overview text.\n\n- Item one\n- Item two';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<h2>OVERVIEW</h2><p>This is the overview text.</p><ul><li><p>Item one</p></li><li><p>Item two</p></li></ul>',
      );
    });

    it('handles heading + numbered list + paragraph', () => {
      const input = 'STEPS\n\n1. Do this\n2. Do that\n\nConclusion text.';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<h2>STEPS</h2><ol><li><p>Do this</p></li><li><p>Do that</p></li></ol><p>Conclusion text.</p>',
      );
    });

    it('handles bullet list followed by numbered list', () => {
      const input = '- Bullet A\n- Bullet B\n\n1. Number one\n2. Number two';
      const result = parseStructuredText(input);
      expect(result).toBe(
        '<ul><li><p>Bullet A</p></li><li><p>Bullet B</p></li></ul><ol><li><p>Number one</p></li><li><p>Number two</p></li></ol>',
      );
    });
  });

  // ── HTML Escaping ─────────────────────────────────────────

  describe('HTML Escaping', () => {
    it('escapes angle brackets in text', () => {
      const input = '- Use <div> tags\n- Avoid <script>';
      const result = parseStructuredText(input);
      expect(result).toContain('&lt;div&gt;');
      expect(result).toContain('&lt;script&gt;');
    });

    it('escapes ampersands', () => {
      const input = '- AT&T\n- R&D';
      const result = parseStructuredText(input);
      expect(result).toContain('AT&amp;T');
      expect(result).toContain('R&amp;D');
    });

    it('escapes quotes in text', () => {
      const input = '- She said "hello"';
      const result = parseStructuredText(input);
      expect(result).toContain('She said &quot;hello&quot;');
    });
  });

  // ── No False Positives ────────────────────────────────────

  describe('No False Positives', () => {
    it('does not treat hyphenated words as bullets', () => {
      const input = 'This is a well-known fact.\nIt is a step-by-step guide.';
      const result = parseStructuredText(input);
      // Should be a single paragraph, not a bullet list
      expect(result).not.toContain('<ul>');
      expect(result).toContain('<p>');
    });

    it('does not treat a dash without trailing space as a bullet', () => {
      const input = '-no space here\n-another no space';
      const result = parseStructuredText(input);
      expect(result).not.toContain('<ul>');
      expect(result).toContain('<p>');
    });

    it('does not treat a number in a sentence as a numbered list', () => {
      const input = 'There are 3 reasons for this.';
      const result = parseStructuredText(input);
      expect(result).not.toContain('<ol>');
      expect(result).toBe('<p>There are 3 reasons for this.</p>');
    });
  });

  // ── Edge Cases ────────────────────────────────────────────

  describe('Edge Cases', () => {
    it('returns empty string for empty input', () => {
      expect(parseStructuredText('')).toBe('');
    });

    it('returns empty string for whitespace-only input', () => {
      expect(parseStructuredText('   \n  \n   ')).toBe('');
    });

    it('handles single line of plain text', () => {
      const result = parseStructuredText('Just a single line.');
      expect(result).toBe('<p>Just a single line.</p>');
    });

    it('handles multiple blank lines between content', () => {
      const input = '- Item A\n\n\n\n- Item B';
      const result = parseStructuredText(input);
      // Blank lines break the list into two separate lists
      expect(result).toContain('<ul>');
      expect(result).toContain('Item A');
      expect(result).toContain('Item B');
    });
  });
});
