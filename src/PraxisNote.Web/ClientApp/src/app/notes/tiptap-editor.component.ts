import {
  Component,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ElementRef,
  inject,
  input,
  output,
  signal,
  OnDestroy,
  OnInit,
  AfterViewInit,
  effect,
  viewChild,
} from '@angular/core';
import { Editor } from '@tiptap/core';
import StarterKit from '@tiptap/starter-kit';
import TaskList from '@tiptap/extension-task-list';
import TaskItem from '@tiptap/extension-task-item';
import Placeholder from '@tiptap/extension-placeholder';
import { TiptapEditorDirective } from 'ngx-tiptap';
import { CheckboxStatus } from './note.model';

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
    <div class="tiptap-editor-wrapper" #editorWrapper>
      <tiptap-editor [editor]="editor"></tiptap-editor>

      <!-- Overlay for promote buttons and status badges (outside TipTap's control) -->
      <div class="checkbox-overlay">
        @for (item of checkboxOverlayItems(); track item.index) {
          @if (item.isLinked) {
            <!-- Status badge for linked checkboxes -->
            <span
              class="status-badge"
              [class.status-todo]="item.taskStatus === 'Todo'"
              [class.status-inprogress]="item.taskStatus === 'InProgress'"
              [class.status-done]="item.taskStatus === 'Done'"
              [style.top.px]="item.top"
            >
              {{ item.taskStatus === 'InProgress' ? 'In Progress' : item.taskStatus }}
            </span>
          } @else {
            <!-- Promote button for unlinked checkboxes -->
            <button
              type="button"
              class="promote-overlay-btn"
              [style.top.px]="item.top"
              (click)="onPromoteClick(item.index)"
              title="Promote to task"
              aria-label="Promote checkbox to task"
            >
              <i class="pi pi-arrow-right"></i>
              <span>Task</span>
            </button>
          }
        }
      </div>
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
      position: relative;
      min-height: 200px;
      max-height: 400px;
      overflow-y: auto;
      padding: 0.75rem;
      background: var(--color-surface-subtle);
      border-radius: 0 0 6px 6px;
    }

    /* Overlay for promote buttons and status badges (outside TipTap's control) */
    .checkbox-overlay {
      position: absolute;
      top: 0;
      right: 0.75rem;
      width: 0;
      height: 100%;
      pointer-events: none;
    }

    .promote-overlay-btn {
      position: absolute;
      right: 0;
      display: flex;
      align-items: center;
      gap: 0.25em;
      padding: 0.2em 0.5em;
      border-radius: 4px;
      font-size: 11px;
      background: transparent;
      color: var(--color-accent-solid);
      border: none;
      cursor: pointer;
      pointer-events: auto;
      opacity: 0;
      transition: opacity 0.15s, background 0.15s;
    }

    .promote-overlay-btn i {
      font-size: 10px;
    }

    .tiptap-editor-wrapper:hover .promote-overlay-btn {
      opacity: 1;
    }

    .promote-overlay-btn:hover {
      background: var(--color-accent-subtle);
    }

    /* Status badges for linked checkboxes */
    .status-badge {
      position: absolute;
      right: 0;
      display: inline-flex;
      align-items: center;
      padding: 2px 6px;
      border-radius: 4px;
      font-size: 10px;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.025em;
      white-space: nowrap;
      pointer-events: auto;
    }

    .status-todo {
      background: var(--color-todo-text);
      color: white;
    }

    .status-inprogress {
      background: var(--color-inprogress-text);
      color: white;
    }

    .status-done {
      background: var(--color-done-text);
      color: white;
    }

    /* Mobile: always show buttons */
    @media (hover: none) {
      .promote-overlay-btn {
        opacity: 1;
      }
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
      position: relative;
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
      background: var(--color-accent-solid);
      color: white;
    }

    :host ::ng-deep .ProseMirror *::selection {
      background: var(--color-accent-solid);
      color: white;
    }
  `],
})
export class TiptapEditorComponent implements OnInit, OnDestroy, AfterViewInit {
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly elementRef = inject(ElementRef);

  /** Initial content (JSON string or empty string for new notes) */
  readonly initialContent = input<string>('');

  /** Whether this is a new note (will start with heading format) */
  readonly isNewNote = input<boolean>(false);

  /** Trigger to force editor reset (incremented each time dialog opens) */
  readonly resetTrigger = input<number>(0);

  /** Emits when content changes */
  readonly contentChange = output<string>();

  /** Emits when user clicks promote button on a checkbox */
  readonly promoteCheckbox = output<{ checkboxIndex: number }>();

  /** Checkbox status data for showing linked state */
  readonly checkboxStatuses = input<CheckboxStatus[]>([]);

  /** Reference to the editor wrapper for position calculations */
  private readonly editorWrapper = viewChild<ElementRef>('editorWrapper');

  /** Computed overlay items for promote buttons and status badges */
  readonly checkboxOverlayItems = signal<Array<{
    index: number;
    top: number;
    isLinked: boolean;
    taskStatus: 'Todo' | 'InProgress' | 'Done' | null;
  }>>([]);

  /** Track if we're initializing to avoid emitting on load */
  private isInitializing = true;
  private hasInitialized = false;

  /** Pending setTimeout ID for coalescing button injection calls */
  private pendingButtonInjection: ReturnType<typeof setTimeout> | null = null;

  /** MutationObserver to update overlay when TipTap re-renders */
  private mutationObserver: MutationObserver | null = null;

  /** Scroll handler reference for cleanup */
  private scrollHandler: (() => void) | null = null;

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
    onCreate: () => {
      // Inject buttons after editor is created and DOM is ready
      this.scheduleOverlayUpdate();
    },
    onUpdate: ({ editor }) => {
      if (!this.isInitializing) {
        const json = editor.getJSON();
        this.contentChange.emit(JSON.stringify(json));
      }
      // Always re-inject after content updates (TipTap re-renders the DOM)
      this.scheduleOverlayUpdate();
    },
    onSelectionUpdate: () => {
      // Trigger change detection to update toolbar active states
      this.cdr.markForCheck();
    },
  });

  constructor() {
    // Watch for initialContent, isNewNote, and resetTrigger changes after first init
    effect(() => {
      const content = this.initialContent();
      const isNew = this.isNewNote();
      // Read resetTrigger to ensure effect re-runs when it changes
      this.resetTrigger();
      // Only react to changes after initial setup
      if (this.hasInitialized) {
        this.setEditorContent(content, isNew);
      }
    });

    // Re-inject buttons whenever checkbox statuses change
    effect(() => {
      this.checkboxStatuses(); // Subscribe to changes
      // Only inject after initial setup is complete
      if (this.hasInitialized) {
        this.scheduleOverlayUpdate();
      }
    });
  }

  ngOnInit(): void {
    // Set initial content once on init
    this.setEditorContent(this.initialContent(), this.isNewNote());
    this.hasInitialized = true;
  }

  ngAfterViewInit(): void {
    // Set up MutationObserver to re-inject buttons when TipTap re-renders the DOM
    this.setupMutationObserver();
    // Set up scroll listener to update overlay positions when editor scrolls
    this.setupScrollListener();
    // Initial injection of promote buttons after view is ready
    this.scheduleOverlayUpdate();
  }

  /**
   * Sets up a scroll listener on the editor wrapper to keep overlay positions in sync.
   */
  private setupScrollListener(): void {
    const wrapper = this.editorWrapper()?.nativeElement as HTMLElement;
    if (!wrapper) return;

    this.scrollHandler = () => this.scheduleOverlayUpdate();
    wrapper.addEventListener('scroll', this.scrollHandler, { passive: true });
  }

  /**
   * Sets up a MutationObserver to detect when TipTap re-renders task items.
   * Re-injects promote buttons when task items are modified.
   */
  private setupMutationObserver(): void {
    const container = this.elementRef.nativeElement as HTMLElement;
    const proseMirror = container.querySelector('.ProseMirror');
    if (!proseMirror) return;

    this.mutationObserver = new MutationObserver((mutations) => {
      // Check if any mutation affects task list items
      const affectsTaskList = mutations.some(m =>
        m.type === 'childList' &&
        m.target instanceof Element &&
        m.target.closest('ul[data-type="taskList"]')
      );
      if (affectsTaskList) {
        this.scheduleOverlayUpdate();
      }
    });

    this.mutationObserver.observe(proseMirror, {
      childList: true,
      subtree: true,
    });
  }

  /**
   * Schedules an overlay update with a debounce.
   * Uses setTimeout to let TipTap complete its rendering cycle before calculating positions.
   */
  private scheduleOverlayUpdate(): void {
    if (this.pendingButtonInjection !== null) {
      clearTimeout(this.pendingButtonInjection);
    }
    // Use setTimeout with delay to ensure TipTap has finished rendering
    this.pendingButtonInjection = setTimeout(() => {
      this.pendingButtonInjection = null;
      this.updateCheckboxOverlay();
    }, 50);
  }

  /**
   * Injects promote buttons and status badges into task items.
   * Called after editor updates and when checkbox statuses change.
   */
  /**
   * Computes overlay positions for promote buttons.
   * Instead of injecting into TipTap's DOM (which gets re-rendered),
   * we calculate positions and render buttons via Angular template.
   */
  private updateCheckboxOverlay(): void {
    const wrapper = this.editorWrapper()?.nativeElement as HTMLElement;
    if (!wrapper) {
      this.checkboxOverlayItems.set([]);
      return;
    }

    const taskItems = wrapper.querySelectorAll<HTMLElement>(
      '.ProseMirror ul[data-type="taskList"] > li'
    );

    if (taskItems.length === 0) {
      this.checkboxOverlayItems.set([]);
      return;
    }

    // Build Map for O(1) lookups
    const statusMap = new Map(
      this.checkboxStatuses().map(s => [s.checkboxId, s])
    );

    const wrapperRect = wrapper.getBoundingClientRect();
    const scrollTop = wrapper.scrollTop;
    // Offset to align badge/button with checkbox text baseline
    const VERTICAL_ALIGNMENT_OFFSET = 2;
    const items: Array<{
      index: number;
      top: number;
      isLinked: boolean;
      taskStatus: 'Todo' | 'InProgress' | 'Done' | null;
    }> = [];

    taskItems.forEach((item, index) => {
      const checkboxId = `cb-${index + 1}`;
      const status = statusMap.get(checkboxId);
      const itemRect = item.getBoundingClientRect();

      items.push({
        index,
        // Calculate position relative to wrapper's scroll position
        top: itemRect.top - wrapperRect.top + scrollTop + VERTICAL_ALIGNMENT_OFFSET,
        isLinked: status?.isLinked ?? false,
        taskStatus: status?.taskStatus ?? null,
      });
    });

    this.checkboxOverlayItems.set(items);
    this.cdr.markForCheck();
  }

  /** Handle promote button click from overlay */
  onPromoteClick(checkboxIndex: number): void {
    this.promoteCheckbox.emit({ checkboxIndex });
  }

  private setEditorContent(content: string, isNewNote: boolean): void {
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
    } else if (isNewNote) {
      // For new notes, start with heading format so user can type a title immediately
      this.editor.commands.clearContent();
      this.editor.commands.setHeading({ level: 2 });
    } else {
      this.editor.commands.clearContent();
    }

    // Allow updates after initialization
    setTimeout(() => {
      this.isInitializing = false;
    }, 0);
  }

  ngOnDestroy(): void {
    if (this.pendingButtonInjection !== null) {
      clearTimeout(this.pendingButtonInjection);
    }
    if (this.mutationObserver) {
      this.mutationObserver.disconnect();
    }
    if (this.scrollHandler) {
      const wrapper = this.editorWrapper()?.nativeElement as HTMLElement;
      wrapper?.removeEventListener('scroll', this.scrollHandler);
    }
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

  /** Focus the editor at the start */
  focus(): void {
    this.editor.commands.focus('start');
  }
}
