import { Editor } from '@tiptap/core';
import { formatShortcut } from '../../shared/keyboard-utils';
import { normalizeImageUrl } from '../../shared/url-utils';
import { toISODate } from './insert-date.extension';

export interface SlashCommandItem {
  label: string;
  icon: string;
  group: string;
  shortcut?: string;
  aliases?: string[];
  action: (editor: Editor) => void;
}

export const slashCommandItems: SlashCommandItem[] = [
  // Headings
  {
    label: 'Heading 1',
    icon: 'pi pi-hashtag',
    group: 'Headings',
    aliases: ['h1'],
    shortcut: formatShortcut({ mod: true, alt: true, key: '1' }),
    action: (editor) => editor.chain().focus().setHeading({ level: 1 }).run(),
  },
  {
    label: 'Heading 2',
    icon: 'pi pi-hashtag',
    group: 'Headings',
    aliases: ['h2'],
    shortcut: formatShortcut({ mod: true, alt: true, key: '2' }),
    action: (editor) => editor.chain().focus().setHeading({ level: 2 }).run(),
  },
  {
    label: 'Heading 3',
    icon: 'pi pi-hashtag',
    group: 'Headings',
    aliases: ['h3'],
    shortcut: formatShortcut({ mod: true, alt: true, key: '3' }),
    action: (editor) => editor.chain().focus().setHeading({ level: 3 }).run(),
  },

  // Lists
  {
    label: 'Bullet List',
    icon: 'pi pi-list',
    group: 'Lists',
    aliases: ['ul', 'bullets'],
    shortcut: formatShortcut({ mod: true, shift: true, key: '8' }),
    action: (editor) => editor.chain().focus().toggleBulletList().run(),
  },
  {
    label: 'Numbered List',
    icon: 'pi pi-sort-numeric-down',
    group: 'Lists',
    aliases: ['ol', 'numbers'],
    shortcut: formatShortcut({ mod: true, shift: true, key: '7' }),
    action: (editor) => editor.chain().focus().toggleOrderedList().run(),
  },
  {
    label: 'Task List',
    icon: 'pi pi-check-square',
    group: 'Lists',
    aliases: ['task', 'checklist'],
    shortcut: formatShortcut({ mod: true, shift: true, key: '9' }),
    action: (editor) => editor.chain().focus().toggleTaskList().run(),
  },

  // Blocks
  {
    label: 'Toggle Section',
    icon: 'pi pi-chevron-down',
    group: 'Blocks',
    aliases: ['toggle', 'collapse', 'details'],
    action: (editor) => editor.chain().focus().setDetails().run(),
  },
  {
    label: 'Blockquote',
    icon: 'pi pi-comment',
    group: 'Blocks',
    aliases: ['quote', 'bq'],
    shortcut: formatShortcut({ mod: true, shift: true, key: 'B' }),
    action: (editor) => editor.chain().focus().toggleBlockquote().run(),
  },
  {
    label: 'Code Block',
    icon: 'pi pi-code',
    group: 'Blocks',
    aliases: ['code', 'cb'],
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
    aliases: ['img', 'pic'],
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
    aliases: ['hr', 'line', 'separator'],
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
  {
    label: 'Today',
    icon: 'pi pi-calendar',
    group: 'Insert',
    action: (editor) => {
      editor.commands.insertDate();
    },
  },
  {
    label: 'Tomorrow',
    icon: 'pi pi-calendar-plus',
    group: 'Insert',
    action: (editor) => {
      const tomorrow = new Date();
      tomorrow.setHours(0, 0, 0, 0);
      tomorrow.setDate(tomorrow.getDate() + 1);
      editor.commands.insertDate(toISODate(tomorrow));
    },
  },
];
