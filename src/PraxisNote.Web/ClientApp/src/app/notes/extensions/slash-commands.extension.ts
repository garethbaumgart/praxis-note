import { Extension } from '@tiptap/core';
import { PluginKey } from '@tiptap/pm/state';
import Suggestion, { type SuggestionOptions } from '@tiptap/suggestion';
import { SlashCommandItem, slashCommandItems } from './slash-command-items';

export const SlashCommandsPluginKey = new PluginKey('slashCommands');

export interface SlashCommandsOptions {
  suggestion: Partial<SuggestionOptions<SlashCommandItem, SlashCommandItem>>;
}

export const SlashCommands = Extension.create<SlashCommandsOptions>({
  name: 'slashCommands',

  addOptions() {
    return {
      suggestion: {
        char: '/',
        allowSpaces: false,
        startOfLine: false,
        pluginKey: SlashCommandsPluginKey,
        items: ({ query }) => {
          if (!query) return slashCommandItems;
          const lower = query.toLowerCase();
          return slashCommandItems.filter(
            (item) =>
              item.label.toLowerCase().includes(lower) ||
              item.group.toLowerCase().includes(lower) ||
              (item.aliases?.some((a) => a.toLowerCase().includes(lower)) ?? false),
          );
        },
        command: ({ editor, range, props }) => {
          // Delete the slash command trigger text (e.g., "/hea")
          editor.chain().focus().deleteRange(range).run();
          // Execute the selected command's action
          props.action(editor);
        },
      },
    };
  },

  addProseMirrorPlugins() {
    return [
      Suggestion<SlashCommandItem, SlashCommandItem>({
        editor: this.editor,
        ...this.options.suggestion,
      }),
    ];
  },
});
