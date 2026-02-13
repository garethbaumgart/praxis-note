import { Extension } from '@tiptap/core';
import { formatShortDate } from '../../shared/date-utils';

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    insertDate: {
      insertDate: () => ReturnType;
    };
  }
}

export const InsertDate = Extension.create({
  name: 'insertDate',

  addCommands() {
    return {
      insertDate:
        () =>
        ({ chain }) => {
          const dateText = formatShortDate(new Date());
          return chain().insertContent({ type: 'text', text: dateText }).run();
        },
    };
  },

  addKeyboardShortcuts() {
    return {
      'Mod-Shift-d': () => this.editor.commands.insertDate(),
    };
  },
});
