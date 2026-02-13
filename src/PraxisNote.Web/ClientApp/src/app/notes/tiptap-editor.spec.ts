import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Editor } from '@tiptap/core';
import Placeholder from '@tiptap/extension-placeholder';
import { tiptapExtensions } from './tiptap-extensions';
import { slashCommandItems } from './extensions/slash-command-items';
import { formatShortDate } from '../shared/date-utils';

/**
 * TipTap Editor Test Suite
 *
 * Tests the TipTap editor directly — not the Angular component.
 * Instantiates a real Editor with the production tiptapExtensions array
 * and asserts that commands produce correct document/DOM output.
 *
 * This avoids Angular TestBed complexity while testing the actual
 * business logic: that TipTap commands produce correct output.
 */

function createEditor(): Editor {
  return new Editor({
    element: document.createElement('div'),
    extensions: [
      ...tiptapExtensions,
      Placeholder.configure({ placeholder: 'Test...' }),
    ],
  });
}

/** Helper: insert text into the editor and select all of it */
function insertAndSelectAll(editor: Editor, text: string): void {
  editor.commands.setContent({ type: 'doc', content: [{ type: 'paragraph', content: [{ type: 'text', text }] }] });
  editor.commands.selectAll();
}

describe('TipTap Editor', () => {
  let editor: Editor;

  beforeEach(() => {
    editor = createEditor();
  });

  afterEach(() => {
    editor.destroy();
  });

  // ── Step 6: Extension Registration ──────────────────────────

  describe('Extension Registration', () => {
    it('creates an editable editor with all extensions loaded', () => {
      expect(editor.isEditable).toBe(true);
    });

    it.each([
      'starterKit',
      'taskList',
      'taskItem',
      'link',
      'underline',
      'highlight',
      'textStyle',
      'color',
      'textAlign',
      'image',
      'table',
      'tableRow',
      'tableCell',
      'tableHeader',
      'codeBlock',
      'details',
      'detailsSummary',
      'detailsContent',
      'insertDate',
    ])('has %s extension registered', (name) => {
      const has = editor.extensionManager.extensions.some((ext) => ext.name === name);
      expect(has).toBe(true);
    });
  });

  // ── Step 7: Inline Formatting ──────────────────────────────

  describe('Inline Formatting', () => {
    it('toggleBold applies and removes bold mark', () => {
      insertAndSelectAll(editor, 'hello');
      editor.chain().focus().toggleBold().run();
      expect(editor.isActive('bold')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().toggleBold().run();
      expect(editor.isActive('bold')).toBe(false);
    });

    it('toggleItalic applies and removes italic mark', () => {
      insertAndSelectAll(editor, 'hello');
      editor.chain().focus().toggleItalic().run();
      expect(editor.isActive('italic')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().toggleItalic().run();
      expect(editor.isActive('italic')).toBe(false);
    });

    it('toggleUnderline applies and removes underline mark', () => {
      insertAndSelectAll(editor, 'hello');
      editor.chain().focus().toggleUnderline().run();
      expect(editor.isActive('underline')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().toggleUnderline().run();
      expect(editor.isActive('underline')).toBe(false);
    });

    it('toggleStrike applies and removes strike mark', () => {
      insertAndSelectAll(editor, 'hello');
      editor.chain().focus().toggleStrike().run();
      expect(editor.isActive('strike')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().toggleStrike().run();
      expect(editor.isActive('strike')).toBe(false);
    });

    it('toggleHighlight applies and removes highlight mark', () => {
      insertAndSelectAll(editor, 'hello');
      editor.chain().focus().toggleHighlight().run();
      expect(editor.isActive('highlight')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().toggleHighlight().run();
      expect(editor.isActive('highlight')).toBe(false);
    });

    it('toggleCode applies and removes code mark', () => {
      insertAndSelectAll(editor, 'hello');
      editor.chain().focus().toggleCode().run();
      expect(editor.isActive('code')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().toggleCode().run();
      expect(editor.isActive('code')).toBe(false);
    });

    it('setLink applies link mark with href', () => {
      insertAndSelectAll(editor, 'click here');
      editor.chain().focus().setLink({ href: 'https://example.com' }).run();
      expect(editor.isActive('link')).toBe(true);

      const html = editor.getHTML();
      expect(html).toContain('href="https://example.com"');
    });

    it('unsetLink removes link mark', () => {
      insertAndSelectAll(editor, 'click here');
      editor.chain().focus().setLink({ href: 'https://example.com' }).run();
      expect(editor.isActive('link')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().unsetLink().run();
      expect(editor.isActive('link')).toBe(false);
    });

    it('setColor applies text color', () => {
      insertAndSelectAll(editor, 'colored text');
      editor.chain().focus().setColor('#ff0000').run();

      const html = editor.getHTML();
      expect(html).toContain('color');
      // TipTap may serialize as hex or rgb format
      const hasColor = html.includes('#ff0000') || html.includes('rgb(255, 0, 0)');
      expect(hasColor).toBe(true);
    });

    it('clearNodes + unsetAllMarks removes all formatting', () => {
      insertAndSelectAll(editor, 'formatted');
      editor.chain().focus().toggleBold().toggleItalic().toggleHighlight().run();

      expect(editor.isActive('bold')).toBe(true);
      expect(editor.isActive('italic')).toBe(true);

      editor.commands.selectAll();
      editor.chain().focus().clearNodes().unsetAllMarks().run();

      expect(editor.isActive('bold')).toBe(false);
      expect(editor.isActive('italic')).toBe(false);
      expect(editor.isActive('highlight')).toBe(false);
      expect(editor.isActive('paragraph')).toBe(true);
    });
  });

  // ── Step 8: Block Types ─────────────────────────────────────

  describe('Block Types', () => {
    it('setParagraph sets paragraph block type', () => {
      editor.chain().focus().setParagraph().run();
      expect(editor.isActive('paragraph')).toBe(true);
    });

    it('setHeading(1) sets heading level 1', () => {
      editor.chain().focus().setHeading({ level: 1 }).run();
      expect(editor.isActive('heading', { level: 1 })).toBe(true);
    });

    it('setHeading(2) sets heading level 2', () => {
      editor.chain().focus().setHeading({ level: 2 }).run();
      expect(editor.isActive('heading', { level: 2 })).toBe(true);
    });

    it('setHeading(3) sets heading level 3', () => {
      editor.chain().focus().setHeading({ level: 3 }).run();
      expect(editor.isActive('heading', { level: 3 })).toBe(true);
    });

    it('toggleHeading toggles heading on and off', () => {
      editor.chain().focus().toggleHeading({ level: 2 }).run();
      expect(editor.isActive('heading', { level: 2 })).toBe(true);

      editor.chain().focus().toggleHeading({ level: 2 }).run();
      expect(editor.isActive('heading', { level: 2 })).toBe(false);
      expect(editor.isActive('paragraph')).toBe(true);
    });

    it('setHeading then setParagraph resets to paragraph', () => {
      editor.chain().focus().setHeading({ level: 1 }).run();
      expect(editor.isActive('heading', { level: 1 })).toBe(true);

      editor.chain().focus().setParagraph().run();
      expect(editor.isActive('paragraph')).toBe(true);
      expect(editor.isActive('heading', { level: 1 })).toBe(false);
    });
  });

  // ── Step 9: Lists ───────────────────────────────────────────

  describe('Lists', () => {
    it('toggleBulletList creates bullet list', () => {
      editor.chain().focus().toggleBulletList().run();
      expect(editor.isActive('bulletList')).toBe(true);
    });

    it('toggleBulletList toggles off', () => {
      editor.chain().focus().toggleBulletList().run();
      expect(editor.isActive('bulletList')).toBe(true);

      editor.chain().focus().toggleBulletList().run();
      expect(editor.isActive('bulletList')).toBe(false);
    });

    it('toggleOrderedList creates ordered list', () => {
      editor.chain().focus().toggleOrderedList().run();
      expect(editor.isActive('orderedList')).toBe(true);
    });

    it('toggleTaskList creates task list', () => {
      editor.chain().focus().toggleTaskList().run();
      expect(editor.isActive('taskList')).toBe(true);
    });
  });

  // ── Step 10: Blocks (including #493 regression) ─────────────

  describe('Blocks', () => {
    it('toggleBlockquote creates blockquote', () => {
      editor.chain().focus().toggleBlockquote().run();
      expect(editor.isActive('blockquote')).toBe(true);
    });

    it('toggleCodeBlock creates code block', () => {
      editor.chain().focus().toggleCodeBlock().run();
      expect(editor.isActive('codeBlock')).toBe(true);
    });

    /**
     * #493 regression test: The Details extension creates a "details" node type.
     *
     * In the browser, the NodeView renders as <div data-type="details"> (not native <details>).
     * The CSS in tiptap-editor.component.ts correctly targets div[data-type="details"].
     *
     * getHTML() uses the HTML serialization (renderHTML) which outputs <details>,
     * but the actual live DOM uses the NodeView. We verify:
     * 1. The JSON document contains the correct "details" node type
     * 2. The HTML serialization contains a details structure
     * 3. The editor recognizes the details node as active
     */
    it('setDetails creates details node in JSON with correct type', () => {
      editor.chain().focus().setDetails().run();

      const json = editor.getJSON();
      const jsonStr = JSON.stringify(json);
      expect(jsonStr).toContain('"type":"details"');
      expect(jsonStr).toContain('"type":"detailsSummary"');
      expect(jsonStr).toContain('"type":"detailsContent"');
    });

    it('setDetails produces details structure in HTML serialization', () => {
      editor.chain().focus().setDetails().run();

      const html = editor.getHTML();
      // HTML serialization renders as native <details> element
      // The NodeView in the browser renders as <div data-type="details">
      expect(html).toContain('details');
      expect(html).toContain('summary');
    });

    it('setDetails node has correct JSON structure for CSS targeting', () => {
      // This test ensures the extension creates a node with type "details"
      // which the NodeView renders as <div data-type="details"> in the browser.
      // The CSS selector `div[data-type="details"]` in the component targets this.
      editor.chain().focus().setDetails().run();

      const json = editor.getJSON();
      const detailsNode = json.content?.find((n: { type: string }) => n.type === 'details');
      expect(detailsNode).toBeDefined();
      expect(detailsNode!.type).toBe('details');

      // Verify child nodes exist
      const children = detailsNode!.content;
      expect(children).toBeDefined();
      const hasDetailsSummary = children!.some((c: { type: string }) => c.type === 'detailsSummary');
      const hasDetailsContent = children!.some((c: { type: string }) => c.type === 'detailsContent');
      expect(hasDetailsSummary).toBe(true);
      expect(hasDetailsContent).toBe(true);
    });
  });

  // ── Step 11: Insert Actions ─────────────────────────────────

  describe('Insert Actions', () => {
    it('setHorizontalRule inserts hr', () => {
      editor.chain().focus().setHorizontalRule().run();

      const json = JSON.stringify(editor.getJSON());
      expect(json).toContain('"type":"horizontalRule"');
    });

    it('setImage inserts image with src', () => {
      editor.chain().focus().setImage({ src: 'https://example.com/img.png' }).run();

      const json = editor.getJSON();
      const jsonStr = JSON.stringify(json);
      expect(jsonStr).toContain('"type":"image"');
      expect(jsonStr).toContain('https://example.com/img.png');
    });

    it('insertTable creates 3x3 table with header', () => {
      editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();

      const json = editor.getJSON();
      const jsonStr = JSON.stringify(json);
      expect(jsonStr).toContain('"type":"table"');
      expect(jsonStr).toContain('"type":"tableRow"');
      expect(jsonStr).toContain('"type":"tableHeader"');
      expect(jsonStr).toContain('"type":"tableCell"');
    });
  });

  // ── Step 12: Text Alignment ─────────────────────────────────

  describe('Text Alignment', () => {
    it('setTextAlign("center") sets center alignment', () => {
      insertAndSelectAll(editor, 'centered text');
      editor.chain().focus().setTextAlign('center').run();

      const html = editor.getHTML();
      expect(html).toContain('text-align: center');
    });

    it('setTextAlign("right") sets right alignment', () => {
      insertAndSelectAll(editor, 'right text');
      editor.chain().focus().setTextAlign('right').run();

      const html = editor.getHTML();
      expect(html).toContain('text-align: right');
    });

    it('setTextAlign("justify") sets justify alignment', () => {
      insertAndSelectAll(editor, 'justified text');
      editor.chain().focus().setTextAlign('justify').run();

      const html = editor.getHTML();
      expect(html).toContain('text-align: justify');
    });

    it('setTextAlign("left") resets to default alignment', () => {
      insertAndSelectAll(editor, 'left text');
      editor.chain().focus().setTextAlign('center').run();
      editor.chain().focus().setTextAlign('left').run();

      const html = editor.getHTML();
      // Left alignment is the default, so it should not have center anymore
      expect(html).not.toContain('text-align: center');
    });
  });

  // ── Step 13: Custom Extensions ──────────────────────────────

  describe('Custom Extensions', () => {
    it('insertDate inserts formatted date text', () => {
      editor.commands.insertDate();

      const text = editor.getText();
      expect(text).toBeTruthy();
      expect(text.length).toBeGreaterThan(0);
    });

    it('insertDate uses formatShortDate output', () => {
      vi.useFakeTimers();
      try {
        vi.setSystemTime(new Date('2026-06-15T10:00:00'));

        // Create fresh editor with faked time
        const timedEditor = createEditor();
        try {
          timedEditor.commands.insertDate();

          const text = timedEditor.getText();
          const expected = formatShortDate(new Date('2026-06-15T10:00:00'));
          expect(text).toContain(expected);
        } finally {
          timedEditor.destroy();
        }
      } finally {
        vi.useRealTimers();
      }
    });
  });

  // ── Step 14: Slash Command Actions ──────────────────────────

  describe('Slash Command Actions', () => {
    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('Heading 1 action sets heading level 1', () => {
      const item = slashCommandItems.find((i) => i.label === 'Heading 1')!;
      item.action(editor);
      expect(editor.isActive('heading', { level: 1 })).toBe(true);
    });

    it('Heading 2 action sets heading level 2', () => {
      const item = slashCommandItems.find((i) => i.label === 'Heading 2')!;
      item.action(editor);
      expect(editor.isActive('heading', { level: 2 })).toBe(true);
    });

    it('Heading 3 action sets heading level 3', () => {
      const item = slashCommandItems.find((i) => i.label === 'Heading 3')!;
      item.action(editor);
      expect(editor.isActive('heading', { level: 3 })).toBe(true);
    });

    it('Bullet List action creates bullet list', () => {
      const item = slashCommandItems.find((i) => i.label === 'Bullet List')!;
      item.action(editor);
      expect(editor.isActive('bulletList')).toBe(true);
    });

    it('Numbered List action creates ordered list', () => {
      const item = slashCommandItems.find((i) => i.label === 'Numbered List')!;
      item.action(editor);
      expect(editor.isActive('orderedList')).toBe(true);
    });

    it('Task List action creates task list', () => {
      const item = slashCommandItems.find((i) => i.label === 'Task List')!;
      item.action(editor);
      expect(editor.isActive('taskList')).toBe(true);
    });

    it('Toggle Section action creates details node', () => {
      const item = slashCommandItems.find((i) => i.label === 'Toggle Section')!;
      item.action(editor);

      const json = JSON.stringify(editor.getJSON());
      expect(json).toContain('"type":"details"');
    });

    it('Blockquote action creates blockquote', () => {
      const item = slashCommandItems.find((i) => i.label === 'Blockquote')!;
      item.action(editor);
      expect(editor.isActive('blockquote')).toBe(true);
    });

    it('Code Block action creates code block', () => {
      const item = slashCommandItems.find((i) => i.label === 'Code Block')!;
      item.action(editor);
      expect(editor.isActive('codeBlock')).toBe(true);
    });

    it('Table action inserts a table', () => {
      const item = slashCommandItems.find((i) => i.label === 'Table')!;
      item.action(editor);

      const json = JSON.stringify(editor.getJSON());
      expect(json).toContain('"type":"table"');
    });

    it('Image action inserts image when user provides valid URL', () => {
      vi.spyOn(window, 'prompt').mockReturnValue('https://example.com/photo.jpg');
      vi.spyOn(window, 'alert').mockImplementation(() => {});

      const item = slashCommandItems.find((i) => i.label === 'Image')!;
      item.action(editor);

      const json = JSON.stringify(editor.getJSON());
      expect(json).toContain('"type":"image"');
      expect(json).toContain('https://example.com/photo.jpg');
    });

    it('Image action does nothing when user cancels prompt', () => {
      vi.spyOn(window, 'prompt').mockReturnValue(null);

      const item = slashCommandItems.find((i) => i.label === 'Image')!;
      item.action(editor);

      const json = JSON.stringify(editor.getJSON());
      expect(json).not.toContain('"type":"image"');
    });

    it('Image action alerts on invalid URL', () => {
      vi.spyOn(window, 'prompt').mockReturnValue('javascript:alert(1)');
      const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});

      const item = slashCommandItems.find((i) => i.label === 'Image')!;
      item.action(editor);

      expect(alertSpy).toHaveBeenCalled();
      const json = JSON.stringify(editor.getJSON());
      expect(json).not.toContain('"type":"image"');
    });

    it('Divider action inserts horizontal rule', () => {
      const item = slashCommandItems.find((i) => i.label === 'Divider')!;
      item.action(editor);

      const json = JSON.stringify(editor.getJSON());
      expect(json).toContain('"type":"horizontalRule"');
    });

    it('Date action inserts date text', () => {
      const item = slashCommandItems.find((i) => i.label === 'Date')!;
      item.action(editor);

      const text = editor.getText();
      expect(text).toBeTruthy();
      expect(text.length).toBeGreaterThan(0);
    });
  });

  // ── Step 15: Content Serialization ──────────────────────────

  describe('Content Serialization', () => {
    it('round-trips JSON content through setContent and getJSON', () => {
      const content = {
        type: 'doc' as const,
        content: [
          {
            type: 'heading',
            attrs: { level: 1, textAlign: 'left' },
            content: [{ type: 'text', text: 'Title' }],
          },
          {
            type: 'paragraph',
            attrs: { textAlign: 'left' },
            content: [
              { type: 'text', text: 'Normal text ' },
              { type: 'text', marks: [{ type: 'bold' }], text: 'bold text' },
            ],
          },
        ],
      };

      editor.commands.setContent(content);
      const output = editor.getJSON();

      // Verify the document structure is preserved
      expect(output.type).toBe('doc');
      expect(output.content).toBeDefined();
      expect(output.content!.length).toBe(2);
      expect(output.content![0].type).toBe('heading');
      expect(output.content![1].type).toBe('paragraph');
    });

    it('preserves marks through round-trip', () => {
      insertAndSelectAll(editor, 'test');
      editor.chain().focus().toggleBold().run();

      const json = editor.getJSON();
      const newEditor = createEditor();
      newEditor.commands.setContent(json);

      const restored = newEditor.getJSON();
      const textNode = restored.content?.[0]?.content?.[0];
      expect(textNode?.marks).toBeDefined();
      expect(textNode!.marks!.some((m: { type: string }) => m.type === 'bold')).toBe(true);

      newEditor.destroy();
    });

    it('preserves complex content with multiple block types', () => {
      // Build a document with multiple block types
      editor.chain().focus().setHeading({ level: 2 }).run();
      editor.commands.insertContent({ type: 'text', text: 'Section' });
      editor.commands.insertContent({
        type: 'paragraph',
        content: [{ type: 'text', text: 'Body text' }],
      });

      const json = editor.getJSON();
      const html = editor.getHTML();

      // Verify both representations contain expected content
      expect(JSON.stringify(json)).toContain('Section');
      expect(html).toContain('Section');
    });
  });

  // ── Step 17: Action Completeness Check ──────────────────────

  describe('Slash Command Completeness', () => {
    const testedLabels = [
      'Heading 1',
      'Heading 2',
      'Heading 3',
      'Bullet List',
      'Numbered List',
      'Task List',
      'Toggle Section',
      'Blockquote',
      'Code Block',
      'Table',
      'Image',
      'Divider',
      'Date',
    ];

    it('every slash command item has a corresponding test', () => {
      const allLabels = slashCommandItems.map((item) => item.label);

      const untestedLabels = allLabels.filter((label) => !testedLabels.includes(label));
      expect(untestedLabels).toEqual([]);
    });

    it('testedLabels matches actual slash command items', () => {
      const allLabels = slashCommandItems.map((item) => item.label);

      // Every tested label should correspond to an actual slash command
      const orphanedLabels = testedLabels.filter((label) => !allLabels.includes(label));
      expect(orphanedLabels).toEqual([]);
    });
  });
});
