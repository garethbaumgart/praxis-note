import {
  Component,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  inject,
  input,
  output,
  OnDestroy,
  OnInit,
  effect,
} from '@angular/core';
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import TaskList from '@tiptap/extension-task-list';
import TaskItem from '@tiptap/extension-task-item';
import Placeholder from '@tiptap/extension-placeholder';
import { TiptapEditorDirective } from 'ngx-tiptap';

@Component({
  selector: 'app-tiptap-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TiptapEditorDirective],
  template: `
    <!-- Toolbar -->
    <div class="flex items-center gap-1 p-2 border-b border-border bg-surface-subtle rounded-t-md">
      <!-- Text formatting -->
      <div class="flex items-center gap-0.5">
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('bold')"
          (click)="toggleBold()"
          title="Bold (Ctrl+B)"
          aria-label="Bold"
        >
          <span class="font-bold">B</span>
        </button>
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('italic')"
          (click)="toggleItalic()"
          title="Italic (Ctrl+I)"
          aria-label="Italic"
        >
          <span class="italic">I</span>
        </button>
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('strike')"
          (click)="toggleStrike()"
          title="Strikethrough"
          aria-label="Strikethrough"
        >
          <span class="line-through">S</span>
        </button>
      </div>

      <div class="w-px h-5 bg-border mx-1"></div>

      <!-- Lists -->
      <div class="flex items-center gap-0.5">
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('bulletList')"
          (click)="toggleBulletList()"
          title="Bullet List"
          aria-label="Bullet list"
        >
          <i class="pi pi-list text-sm"></i>
        </button>
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('orderedList')"
          (click)="toggleOrderedList()"
          title="Numbered List"
          aria-label="Numbered list"
        >
          <i class="pi pi-sort-numeric-down text-sm"></i>
        </button>
        <button
          type="button"
          class="toolbar-btn task-list-btn"
          [class.active]="editor.isActive('taskList')"
          (click)="toggleTaskList()"
          title="Task List (Checkbox)"
          aria-label="Task list"
        >
          <i class="pi pi-check-square text-sm"></i>
        </button>
      </div>

      <div class="w-px h-5 bg-border mx-1"></div>

      <!-- Block types -->
      <div class="flex items-center gap-0.5">
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('heading', { level: 2 })"
          (click)="toggleHeading()"
          title="Heading"
          aria-label="Heading"
        >
          <span class="font-semibold text-xs">H</span>
        </button>
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('blockquote')"
          (click)="toggleBlockquote()"
          title="Quote"
          aria-label="Quote"
        >
          <i class="pi pi-comment text-sm"></i>
        </button>
        <button
          type="button"
          class="toolbar-btn"
          [class.active]="editor.isActive('codeBlock')"
          (click)="toggleCodeBlock()"
          title="Code Block"
          aria-label="Code block"
        >
          <i class="pi pi-code text-sm"></i>
        </button>
      </div>
    </div>

    <!-- Editor -->
    <div class="tiptap-editor-wrapper">
      <tiptap-editor [editor]="editor"></tiptap-editor>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }

    .toolbar-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      border-radius: 4px;
      color: var(--color-foreground-secondary);
      transition: all 0.15s;
    }

    .toolbar-btn:hover {
      background: var(--color-surface-hover);
      color: var(--color-foreground-default);
    }

    .toolbar-btn.active {
      background: var(--color-accent-subtle);
      color: var(--color-accent-solid);
    }

    .toolbar-btn.task-list-btn.active {
      background: var(--color-accent-solid);
      color: white;
    }

    .tiptap-editor-wrapper {
      min-height: 200px;
      max-height: 400px;
      overflow-y: auto;
      padding: 0.75rem;
      background: var(--color-surface-subtle);
      border-radius: 0 0 6px 6px;
    }

    /* ProseMirror Editor Styles */
    :host ::ng-deep .ProseMirror {
      outline: none;
      min-height: 180px;
    }

    :host ::ng-deep .ProseMirror p {
      margin: 0.5em 0;
    }

    :host ::ng-deep .ProseMirror p:first-child {
      margin-top: 0;
    }

    :host ::ng-deep .ProseMirror h2 {
      font-size: 1.25em;
      font-weight: 600;
      margin: 1em 0 0.5em;
    }

    :host ::ng-deep .ProseMirror h2:first-child {
      margin-top: 0;
    }

    :host ::ng-deep .ProseMirror ul {
      padding-left: 1.5em;
      margin: 0.5em 0;
      list-style-type: disc;
    }

    :host ::ng-deep .ProseMirror ol {
      padding-left: 1.5em;
      margin: 0.5em 0;
      list-style-type: decimal;
    }

    :host ::ng-deep .ProseMirror li {
      margin: 0.25em 0;
      display: list-item;
    }

    :host ::ng-deep .ProseMirror blockquote {
      border-left: 3px solid var(--color-border-default);
      padding-left: 1em;
      margin: 1em 0;
      color: var(--color-foreground-secondary);
    }

    :host ::ng-deep .ProseMirror code {
      background: var(--color-surface-default);
      padding: 0.2em 0.4em;
      border-radius: 4px;
      font-family: monospace;
      font-size: 0.9em;
    }

    :host ::ng-deep .ProseMirror pre {
      background: var(--color-surface-muted);
      color: var(--color-foreground-default);
      padding: 0.75em 1em;
      border-radius: 6px;
      overflow-x: auto;
      margin: 0.5em 0;
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      font-size: 0.875em;
      line-height: 1.5;
    }

    :host ::ng-deep .ProseMirror pre code {
      background: none;
      padding: 0;
    }

    /* Task List Styles */
    :host ::ng-deep .ProseMirror ul[data-type="taskList"] {
      list-style: none;
      padding-left: 0;
    }

    :host ::ng-deep .ProseMirror ul[data-type="taskList"] li {
      display: flex;
      align-items: flex-start;
      gap: 0.5em;
      margin: 0.25em 0;
    }

    :host ::ng-deep .ProseMirror ul[data-type="taskList"] li > label {
      flex-shrink: 0;
      margin-top: 0.2em;
      user-select: none;
    }

    :host ::ng-deep .ProseMirror ul[data-type="taskList"] li > label input[type="checkbox"] {
      width: 16px;
      height: 16px;
      accent-color: var(--color-accent-solid);
      cursor: pointer;
    }

    :host ::ng-deep .ProseMirror ul[data-type="taskList"] li[data-checked="true"] > div {
      text-decoration: line-through;
      color: var(--color-foreground-muted);
    }

    :host ::ng-deep .ProseMirror ul[data-type="taskList"] li > div {
      flex: 1;
    }

    /* Placeholder */
    :host ::ng-deep .ProseMirror p.is-editor-empty:first-child::before {
      content: attr(data-placeholder);
      float: left;
      color: var(--color-foreground-muted);
      pointer-events: none;
      height: 0;
    }

    /* Selection - ensure text is selectable and disable native drag */
    :host ::ng-deep .ProseMirror {
      cursor: text;
      user-select: text;
      -webkit-user-select: text;
    }

    :host ::ng-deep .ProseMirror p,
    :host ::ng-deep .ProseMirror li,
    :host ::ng-deep .ProseMirror h2,
    :host ::ng-deep .ProseMirror blockquote {
      -webkit-user-drag: none;
      user-drag: none;
    }

    :host ::ng-deep .ProseMirror ::selection {
      background: var(--color-accent-subtle);
    }

    :host ::ng-deep .ProseMirror *::selection {
      background: var(--color-accent-subtle);
    }
  `],
})
export class TiptapEditorComponent implements OnInit, OnDestroy {
  private readonly cdr = inject(ChangeDetectorRef);

  /** Initial content (JSON string or empty string for new notes) */
  readonly initialContent = input<string>('');

  /** Emits when content changes */
  readonly contentChange = output<string>();

  /** Track if we're initializing to avoid emitting on load */
  private isInitializing = true;
  private hasInitialized = false;

  editor = new Editor({
    editable: true,
    extensions: [
      StarterKit.configure({
        heading: {
          levels: [2],
        },
      }),
      TaskList,
      TaskItem.configure({
        nested: true,
      }),
      Placeholder.configure({
        placeholder: 'Take a note...',
      }),
    ],
    onUpdate: ({ editor }) => {
      if (!this.isInitializing) {
        const json = editor.getJSON();
        this.contentChange.emit(JSON.stringify(json));
      }
    },
    onSelectionUpdate: () => {
      // Trigger change detection to update toolbar active states
      this.cdr.markForCheck();
    },
  });

  constructor() {
    // Watch for initialContent changes after first init
    effect(() => {
      const content = this.initialContent();
      // Only react to changes after initial setup
      if (this.hasInitialized) {
        this.setEditorContent(content);
      }
    });
  }

  ngOnInit(): void {
    // Set initial content once on init
    this.setEditorContent(this.initialContent());
    this.hasInitialized = true;
  }

  private setEditorContent(content: string): void {
    this.isInitializing = true;

    if (content) {
      try {
        const parsed = JSON.parse(content);
        this.editor.commands.setContent(parsed);
      } catch {
        // If not valid JSON, treat as plain text and wrap in paragraph
        const doc = {
          type: 'doc',
          content: [
            {
              type: 'paragraph',
              content: [{ type: 'text', text: content }],
            },
          ],
        };
        this.editor.commands.setContent(doc);
      }
    } else {
      this.editor.commands.clearContent();
    }

    // Allow updates after initialization
    setTimeout(() => {
      this.isInitializing = false;
    }, 0);
  }

  ngOnDestroy(): void {
    this.editor.destroy();
  }

  // Toolbar actions
  toggleBold(): void {
    this.editor.chain().focus().toggleBold().run();
  }

  toggleItalic(): void {
    this.editor.chain().focus().toggleItalic().run();
  }

  toggleStrike(): void {
    this.editor.chain().focus().toggleStrike().run();
  }

  toggleBulletList(): void {
    this.editor.chain().focus().toggleBulletList().run();
  }

  toggleOrderedList(): void {
    this.editor.chain().focus().toggleOrderedList().run();
  }

  toggleTaskList(): void {
    this.editor.chain().focus().toggleTaskList().run();
  }

  toggleHeading(): void {
    this.editor.chain().focus().toggleHeading({ level: 2 }).run();
  }

  toggleBlockquote(): void {
    this.editor.chain().focus().toggleBlockquote().run();
  }

  toggleCodeBlock(): void {
    this.editor.chain().focus().toggleCodeBlock().run();
  }

  /** Get current content as JSON string */
  getContent(): string {
    return JSON.stringify(this.editor.getJSON());
  }

  /** Check if editor has meaningful content */
  hasContent(): boolean {
    return !this.editor.isEmpty;
  }
}
