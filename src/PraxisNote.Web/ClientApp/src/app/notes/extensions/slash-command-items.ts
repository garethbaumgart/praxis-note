import { Editor } from '@tiptap/core';
import { formatShortcut } from '../../shared/keyboard-utils';
import { normalizeImageUrl } from '../../shared/url-utils';

export interface SlashCommandItem {
  label: string;
  icon: string;
  group: string;
  shortcut?: string;
  action: (editor: Editor) => void;
}

export const slashCommandItems: SlashCommandItem[] = [
  // Headings
  {
    label: 'Heading 1',
    icon: 'pi pi-hashtag',
    group: 'Headings',
    shortcut: formatShortcut({ mod: true, alt: true, key: '1' }),
    action: (editor) => editor.chain().focus().setHeading({ level: 1 }).run(),
  },
  {
    label: 'Heading 2',
    icon: 'pi pi-hashtag',
    group: 'Headings',
    shortcut: formatShortcut({ mod: true, alt: true, key: '2' }),
    action: (editor) => editor.chain().focus().setHeading({ level: 2 }).run(),
  },
  {
    label: 'Heading 3',
    icon: 'pi pi-hashtag',
    group: 'Headings',
    shortcut: formatShortcut({ mod: true, alt: true, key: '3' }),
    action: (editor) => editor.chain().focus().setHeading({ level: 3 }).run(),
  },

  // Lists
  {
    label: 'Bullet List',
    icon: 'pi pi-list',
    group: 'Lists',
    shortcut: formatShortcut({ mod: true, shift: true, key: '8' }),
    action: (editor) => editor.chain().focus().toggleBulletList().run(),
  },
  {
    label: 'Numbered List',
    icon: 'pi pi-sort-numeric-down',
    group: 'Lists',
    shortcut: formatShortcut({ mod: true, shift: true, key: '7' }),
    action: (editor) => editor.chain().focus().toggleOrderedList().run(),
  },
  {
    label: 'Task List',
    icon: 'pi pi-check-square',
    group: 'Lists',
    shortcut: formatShortcut({ mod: true, shift: true, key: '9' }),
    action: (editor) => editor.chain().focus().toggleTaskList().run(),
  },

  // Blocks
  {
    label: 'Toggle Section',
    icon: 'pi pi-chevron-down',
    group: 'Blocks',
    action: (editor) => editor.chain().focus().setDetails().run(),
  },
  {
    label: 'Blockquote',
    icon: 'pi pi-comment',
    group: 'Blocks',
    shortcut: formatShortcut({ mod: true, shift: true, key: 'B' }),
    action: (editor) => editor.chain().focus().toggleBlockquote().run(),
  },
  {
    label: 'Code Block',
    icon: 'pi pi-code',
    group: 'Blocks',
    shortcut: formatShortcut({ mod: true, alt: true, key: 'C' }),
    action: (editor) => editor.chain().focus().toggleCodeBlock().run(),
  },

  // Insert
  {
    label: 'Table',
    icon: 'pi pi-table',
    group: 'Insert',
    action: (editor) =>
      editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run(),
  },
  {
    label: 'Image',
    icon: 'pi pi-image',
    group: 'Insert',
    action: (editor) => {
      const rawUrl = window.prompt('Enter the image URL:');
      if (!rawUrl) return;
      const normalized = normalizeImageUrl(rawUrl);
      if (!normalized) {
        window.alert('Please enter a valid http or https URL.');
        return;
      }
      editor.chain().focus().setImage({ src: normalized }).run();
    },
  },
  {
    label: 'Divider',
    icon: 'pi pi-minus',
    group: 'Insert',
    action: (editor) => editor.chain().focus().setHorizontalRule().run(),
  },
  {
    label: 'Date',
    icon: 'pi pi-calendar',
    group: 'Insert',
    shortcut: formatShortcut({ mod: true, shift: true, key: 'D' }),
    action: (editor) => {
      editor.commands.insertDate();
    },
  },
];
