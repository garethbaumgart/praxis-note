import { Node, mergeAttributes } from '@tiptap/core';
import { formatShortDate } from '../../shared/date-utils';

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    dateNode: {
      insertDate: (date?: string) => ReturnType;
    };
  }
}

export function toISODate(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export const DateNode = Node.create({
  name: 'dateNode',
  group: 'inline',
  inline: true,
  atom: true,
  selectable: true,

  addAttributes() {
    return {
      date: { default: null },
    };
  },

  parseHTML() {
    return [{ tag: 'span[data-type="dateNode"]' }];
  },

  renderHTML({ HTMLAttributes }) {
    const iso = HTMLAttributes['date'] as string | null;
    const display = iso ? formatShortDate(new Date(iso + 'T00:00:00')) : '';
    return [
      'span',
      mergeAttributes(HTMLAttributes, { 'data-type': 'dateNode', class: 'date-node' }),
      display,
    ];
  },

  addNodeView() {
    return ({ node, editor, getPos }) => {
      const wrapper = document.createElement('span');
      wrapper.classList.add('date-node');
      wrapper.setAttribute('data-type', 'dateNode');
      wrapper.contentEditable = 'false';

      const icon = document.createElement('i');
      icon.className = 'pi pi-calendar';
      wrapper.appendChild(icon);

      const text = document.createElement('span');
      text.className = 'date-node-text';
      wrapper.appendChild(text);

      let popover: HTMLElement | null = null;
      let pickerInput: HTMLInputElement | null = null;

      function formatDisplay(iso: string | null): string {
        if (!iso) return '';
        return formatShortDate(new Date(iso + 'T00:00:00'));
      }

      function updateDisplay() {
        text.textContent = formatDisplay(node.attrs['date']);
      }

      function updateNodeDate(newDate: string) {
        const pos = getPos();
        if (typeof pos === 'number') {
          editor.chain().focus()
            .command(({ tr }) => {
              tr.setNodeMarkup(pos, undefined, { date: newDate });
              return true;
            })
            .run();
        }
      }

      function closePopover() {
        if (popover) {
          popover.remove();
          popover = null;
          pickerInput = null;
        }
        document.removeEventListener('click', onDocumentClick);
        document.removeEventListener('keydown', onEscapeKey);
      }

      function onDocumentClick(e: MouseEvent) {
        if (popover && !popover.contains(e.target as HTMLElement) && !wrapper.contains(e.target as HTMLElement)) {
          closePopover();
        }
      }

      function onEscapeKey(e: KeyboardEvent) {
        if (e.key === 'Escape') {
          closePopover();
        }
      }

      function openPopover() {
        if (popover) { closePopover(); return; }

        popover = document.createElement('div');
        popover.className = 'date-node-popover';

        const quickBar = document.createElement('div');
        quickBar.className = 'date-node-quick-bar';

        const quickOptions = [
          { label: 'Today', offset: 0 },
          { label: 'Tomorrow', offset: 1 },
          { label: 'Next Mon', offset: null },
        ];

        for (const opt of quickOptions) {
          const btn = document.createElement('button');
          btn.type = 'button';
          btn.textContent = opt.label;
          btn.className = 'date-node-quick-btn';
          btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const d = new Date();
            d.setHours(0, 0, 0, 0);
            if (opt.offset !== null) {
              d.setDate(d.getDate() + opt.offset);
            } else {
              const day = d.getDay();
              const daysUntilMon = day === 0 ? 1 : 8 - day;
              d.setDate(d.getDate() + daysUntilMon);
            }
            updateNodeDate(toISODate(d));
            closePopover();
          });
          quickBar.appendChild(btn);
        }
        popover.appendChild(quickBar);

        pickerInput = document.createElement('input');
        pickerInput.type = 'date';
        pickerInput.className = 'date-node-picker-input';
        pickerInput.value = node.attrs['date'] || toISODate(new Date());
        pickerInput.addEventListener('change', () => {
          if (pickerInput?.value) {
            updateNodeDate(pickerInput.value);
            closePopover();
          }
        });
        popover.appendChild(pickerInput);

        wrapper.appendChild(popover);

        setTimeout(() => {
          document.addEventListener('click', onDocumentClick);
          document.addEventListener('keydown', onEscapeKey);
        });
      }

      wrapper.addEventListener('click', (e) => {
        e.stopPropagation();
        e.preventDefault();
        openPopover();
      });

      updateDisplay();

      return {
        dom: wrapper,
        update(updatedNode) {
          if (updatedNode.type.name !== 'dateNode') return false;
          node = updatedNode;
          updateDisplay();
          return true;
        },
        destroy() {
          closePopover();
        },
      };
    };
  },

  addCommands() {
    return {
      insertDate:
        (date?: string) =>
        ({ chain }) => {
          const iso = date ?? toISODate(new Date());
          return chain().insertContent({ type: 'dateNode', attrs: { date: iso } }).run();
        },
    };
  },

  addKeyboardShortcuts() {
    return {
      'Mod-Shift-d': () => this.editor.commands.insertDate(),
    };
  },
});
