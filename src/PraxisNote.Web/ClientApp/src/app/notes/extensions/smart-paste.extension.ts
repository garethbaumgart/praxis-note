import { Extension } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';

const BULLET_RE = /^\s*[•\-*◦▪►·‣]\s+/;
const NUMBERED_RE = /^\s*(?:\d+[.)]\s+|[a-z][.)]\s+|[ivx]+[.)]\s+)/i;
const HEADING_RE = /^[A-Z][A-Z0-9 :&\-/]{2,78}$/;

function escapeHtml(text: string): string {
  return text
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function extractBulletText(line: string): string {
  return line.replace(BULLET_RE, '').trim();
}

function extractNumberedText(line: string): string {
  return line.replace(NUMBERED_RE, '').trim();
}

function isBlank(line: string | undefined): boolean {
  return !line || line.trim() === '';
}

/** Pure function: converts plain text to structured HTML */
export function parseStructuredText(text: string): string {
  const lines = text.split('\n');
  const html: string[] = [];
  let i = 0;

  while (i < lines.length) {
    const line = lines[i];

    // Skip blank lines
    if (isBlank(line)) {
      i++;
      continue;
    }

    // Bullet list: collect consecutive bullet lines
    if (BULLET_RE.test(line)) {
      const items: string[] = [];
      while (i < lines.length && BULLET_RE.test(lines[i])) {
        items.push(extractBulletText(lines[i]));
        i++;
      }
      html.push(`<ul>${items.map((t) => `<li><p>${escapeHtml(t)}</p></li>`).join('')}</ul>`);
      continue;
    }

    // Numbered list: collect consecutive numbered lines
    if (NUMBERED_RE.test(line)) {
      const items: string[] = [];
      while (i < lines.length && NUMBERED_RE.test(lines[i])) {
        items.push(extractNumberedText(lines[i]));
        i++;
      }
      html.push(`<ol>${items.map((t) => `<li><p>${escapeHtml(t)}</p></li>`).join('')}</ol>`);
      continue;
    }

    // ALL CAPS heading: short uppercase line followed by blank line or end of input
    if (HEADING_RE.test(line.trim()) && isBlank(lines[i + 1])) {
      html.push(`<h2>${escapeHtml(line.trim())}</h2>`);
      i++;
      continue;
    }

    // Default: collect consecutive plain lines into a paragraph
    const paraLines: string[] = [];
    while (
      i < lines.length &&
      !isBlank(lines[i]) &&
      !BULLET_RE.test(lines[i]) &&
      !NUMBERED_RE.test(lines[i]) &&
      !(HEADING_RE.test(lines[i].trim()) && isBlank(lines[i + 1]))
    ) {
      paraLines.push(lines[i].trim());
      i++;
    }
    if (paraLines.length > 0) {
      html.push(`<p>${escapeHtml(paraLines.join(' '))}</p>`);
    }
  }

  return html.join('');
}

/** Returns true if the text contains any detectable structure */
function hasStructure(text: string): boolean {
  const lines = text.split('\n');
  return lines.some(
    (line, idx) =>
      BULLET_RE.test(line) || NUMBERED_RE.test(line) || (HEADING_RE.test(line.trim()) && isBlank(lines[idx + 1])),
  );
}

/** TipTap extension that intercepts plain text paste and applies heuristic parsing */
export const SmartPaste = Extension.create({
  name: 'smartPaste',

  addProseMirrorPlugins() {
    const editor = this.editor;

    return [
      new Plugin({
        key: new PluginKey('smartPaste'),
        props: {
          handlePaste(_view, event) {
            const clipboardData = event.clipboardData;
            if (!clipboardData) return false;

            // If HTML is on the clipboard, let TipTap's default handler work
            const html = clipboardData.getData('text/html');
            if (html) return false;

            // Plain text only — check if it has detectable structure
            const text = clipboardData.getData('text/plain');
            if (!text || !hasStructure(text)) return false;

            // Parse and insert as structured content
            const structuredHtml = parseStructuredText(text);
            editor.commands.insertContent(structuredHtml);
            return true;
          },
        },
      }),
    ];
  },
});
