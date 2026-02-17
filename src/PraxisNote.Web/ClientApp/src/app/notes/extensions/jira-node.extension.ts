import { Node, mergeAttributes } from '@tiptap/core';
import { Plugin, PluginKey } from '@tiptap/pm/state';

declare module '@tiptap/core' {
  interface Commands<ReturnType> {
    jiraNode: {
      insertJiraLink: (attrs: {
        key: string;
        summary: string;
        status: string;
        statusCategory: string;
        issueType: string;
        url: string;
      }) => ReturnType;
    };
  }
}

/**
 * Regex to detect Jira Cloud URLs.
 * Matches: https://<org>.atlassian.net/browse/PROJ-123
 */
const JIRA_URL_REGEX = /https:\/\/[a-zA-Z0-9-]+\.atlassian\.net\/browse\/([A-Z][A-Z0-9]+-\d+)/;

/**
 * Extracts a Jira issue key from a URL if it matches the Jira pattern.
 */
export function extractJiraKey(url: string): string | null {
  const match = url.trim().match(JIRA_URL_REGEX);
  return match ? match[1] : null;
}

/**
 * Truncates a string to the given length, appending an ellipsis if needed.
 */
function truncate(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength) + '\u2026';
}

/**
 * Maps a Jira issue type name to a short CSS class suffix.
 */
function issueTypeClass(type: string): string {
  const lower = type.toLowerCase();
  if (lower === 'bug') return 'bug';
  if (lower === 'story') return 'story';
  if (lower === 'epic') return 'epic';
  return 'task';
}

/**
 * Maps a Jira status category key to a CSS class suffix.
 */
function statusCategoryClass(category: string): string {
  const lower = category.toLowerCase();
  if (lower === 'indeterminate' || lower === 'in_progress') return 'progress';
  if (lower === 'done') return 'done';
  return 'todo';
}

/**
 * Maps an issue type to a PrimeIcons icon class.
 */
function issueTypeIcon(type: string): string {
  const lower = type.toLowerCase();
  if (lower === 'bug') return 'pi pi-exclamation-circle';
  if (lower === 'story') return 'pi pi-bookmark';
  if (lower === 'epic') return 'pi pi-bolt';
  if (lower === 'subtask' || lower === 'sub-task') return 'pi pi-minus';
  return 'pi pi-check-square';
}

export const JiraNode = Node.create({
  name: 'jiraNode',
  group: 'inline',
  inline: true,
  atom: true,
  selectable: true,

  addAttributes() {
    return {
      key: { default: null },
      summary: { default: '' },
      status: { default: '' },
      statusCategory: { default: 'new' },
      issueType: { default: 'Task' },
      url: { default: '' },
    };
  },

  parseHTML() {
    return [{ tag: 'span[data-type="jiraNode"]' }];
  },

  renderHTML({ HTMLAttributes }) {
    return [
      'span',
      mergeAttributes(HTMLAttributes, { 'data-type': 'jiraNode', class: 'jira-node' }),
      HTMLAttributes['key'] ?? '',
    ];
  },

  addNodeView() {
    return ({ node }) => {
      const wrapper = document.createElement('span');
      wrapper.classList.add('jira-node');
      wrapper.setAttribute('data-type', 'jiraNode');
      wrapper.contentEditable = 'false';

      const issueType = (node.attrs['issueType'] as string) || 'Task';
      const statusCat = (node.attrs['statusCategory'] as string) || 'new';

      // Type icon
      const typeIcon = document.createElement('i');
      typeIcon.className = `jira-node-type-icon jira-type-${issueTypeClass(issueType)} ${issueTypeIcon(issueType)}`;
      typeIcon.setAttribute('aria-hidden', 'true');
      wrapper.appendChild(typeIcon);

      // Key
      const keySpan = document.createElement('span');
      keySpan.className = 'jira-node-key';
      keySpan.textContent = (node.attrs['key'] as string) || '';
      wrapper.appendChild(keySpan);

      // Summary (truncated)
      const summarySpan = document.createElement('span');
      summarySpan.className = 'jira-node-summary';
      summarySpan.textContent = truncate((node.attrs['summary'] as string) || '', 40);
      wrapper.appendChild(summarySpan);

      // Status badge
      const statusBadge = document.createElement('span');
      statusBadge.className = `jira-node-status jira-status-${statusCategoryClass(statusCat)}`;
      statusBadge.textContent = (node.attrs['status'] as string) || '';
      wrapper.appendChild(statusBadge);

      // Click opens the issue URL
      const url = (node.attrs['url'] as string) || '';
      if (url) {
        wrapper.style.cursor = 'pointer';
        wrapper.setAttribute('role', 'link');
        wrapper.setAttribute('aria-label', `Jira issue ${node.attrs['key']}: ${node.attrs['summary']}`);
        wrapper.addEventListener('click', (e) => {
          e.preventDefault();
          e.stopPropagation();
          window.open(url, '_blank', 'noopener,noreferrer');
        });
      }

      return {
        dom: wrapper,
      };
    };
  },

  addCommands() {
    return {
      insertJiraLink:
        (attrs) =>
        ({ chain }) => {
          return chain()
            .insertContent({ type: 'jiraNode', attrs })
            .run();
        },
    };
  },

  addProseMirrorPlugins() {
    const nodeType = this.type;

    return [
      new Plugin({
        key: new PluginKey('jiraPasteHandler'),
        props: {
          handlePaste(view, event) {
            const text = event.clipboardData?.getData('text/plain')?.trim();
            if (!text) return false;

            const key = extractJiraKey(text);
            if (!key) return false;

            // Insert the jira node with the key and URL immediately,
            // other attrs will be populated when the node resolves
            const node = nodeType.create({
              key,
              summary: 'Loading...',
              status: '',
              statusCategory: 'new',
              issueType: 'Task',
              url: text,
            });

            const { tr } = view.state;
            const pos = tr.selection.from;
            tr.replaceSelectionWith(node);
            view.dispatch(tr);

            return true;
          },
        },
      }),
    ];
  },
});
