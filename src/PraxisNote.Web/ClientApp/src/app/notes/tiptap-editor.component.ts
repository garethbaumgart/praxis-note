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
  computed,
} from '@angular/core';
import { Editor } from '@tiptap/core';
import Placeholder from '@tiptap/extension-placeholder';
import { TiptapEditorDirective } from 'ngx-tiptap';
import { CheckboxStatus } from './note.model';
import { tiptapExtensions } from './tiptap-extensions';
import { Select } from 'primeng/select';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { FormsModule } from '@angular/forms';

// Block type options for the dropdown
interface BlockType {
  label: string;
  value: string;
  icon?: string;
  style?: string;
}

@Component({
  selector: 'app-tiptap-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TiptapEditorDirective, Select, Menu, FormsModule],
  host: {
    '[class.expandable]': 'expandable()',
  },
  template: `
    <!-- Toolbar -->
    <div class="toolbar-container" #toolbarContainer>
      <!-- Row 1: Block type + Text formatting + Primary actions -->
      <div class="toolbar-row">
        <!-- Block Type Dropdown -->
        <p-select
          [options]="blockTypes"
          [ngModel]="currentBlockType()"
          (ngModelChange)="onBlockTypeChange($event)"
          optionLabel="label"
          optionValue="value"
          [style]="{ width: '140px' }"
          styleClass="block-type-dropdown"
          appendTo="body"
        >
          <ng-template #selectedItem let-selected>
            <div class="flex items-center gap-2">
              @if (selected?.icon) {
                <i [class]="selected.icon + ' text-xs'"></i>
              }
              <span [style]="selected?.style">{{ selected?.label }}</span>
            </div>
          </ng-template>
          <ng-template #item let-option>
            <div class="flex items-center gap-2 py-1">
              @if (option.icon) {
                <i [class]="option.icon + ' text-sm w-4'"></i>
              } @else {
                <span class="w-4"></span>
              }
              <span [style]="option.style">{{ option.label }}</span>
            </div>
          </ng-template>
        </p-select>

        <div class="divider"></div>

        <!-- Text formatting (always visible) -->
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
          [class.active]="editor.isActive('underline')"
          (click)="toggleUnderline()"
          title="Underline (Ctrl+U)"
          aria-label="Underline"
        >
          <span class="underline">U</span>
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

        @if (toolbarLevel() < 3) {
          <div class="divider"></div>

          <!-- Link & Highlight -->
          <button
            type="button"
            class="toolbar-btn"
            [class.active]="editor.isActive('link')"
            (click)="toggleLink()"
            title="Link"
            aria-label="Insert link"
          >
            <i class="pi pi-link text-sm"></i>
          </button>
          <button
            type="button"
            class="toolbar-btn"
            [class.active]="editor.isActive('highlight')"
            (click)="toggleHighlight()"
            title="Highlight"
            aria-label="Highlight text"
          >
            <i class="pi pi-sun text-sm"></i>
          </button>
          <button
            type="button"
            class="toolbar-btn color-btn"
            (click)="showColorPicker()"
            title="Text Color"
            aria-label="Text color"
          >
            <span class="color-indicator" [style.background]="currentTextColor()"></span>
            <span>A</span>
          </button>
        }

        @if (toolbarLevel() < 2) {
          <div class="divider"></div>

          <!-- Text Align -->
          <button
            type="button"
            class="toolbar-btn"
            [class.active]="editor.isActive({ textAlign: 'left' })"
            (click)="setTextAlign('left')"
            title="Align Left"
            aria-label="Align left"
          >
            <i class="pi pi-align-left text-sm"></i>
          </button>
          <button
            type="button"
            class="toolbar-btn"
            [class.active]="editor.isActive({ textAlign: 'center' })"
            (click)="setTextAlign('center')"
            title="Align Center"
            aria-label="Align center"
          >
            <i class="pi pi-align-center text-sm"></i>
          </button>
          <button
            type="button"
            class="toolbar-btn"
            [class.active]="editor.isActive({ textAlign: 'right' })"
            (click)="setTextAlign('right')"
            title="Align Right"
            aria-label="Align right"
          >
            <i class="pi pi-align-right text-sm"></i>
          </button>
        }

        @if (toolbarLevel() < 4) {
          <div class="divider"></div>

          <!-- Lists -->
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
        }

        @if (toolbarLevel() < 1) {
          <div class="divider"></div>

          <!-- Quote & Code Block -->
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
        }

        <div class="divider"></div>

        <!-- Overflow Menu -->
        <button
          type="button"
          class="toolbar-btn"
          (click)="overflowMenu.toggle($event)"
          title="More options"
          aria-label="More formatting options"
        >
          <i class="pi pi-ellipsis-h text-sm"></i>
        </button>

        <p-menu #overflowMenu [model]="dynamicOverflowMenuItems()" [popup]="true" appendTo="body" />
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

    <!-- Hidden color input for color picker -->
    <input
      #colorInput
      type="color"
      class="hidden-color-input"
      [value]="currentTextColor()"
      (input)="onColorChange($event)"
    />
  `,
  styles: [`
    :host {
      display: block;
    }

    :host(.expandable) {
      display: flex;
      flex-direction: column;
      flex: 1;
      min-height: 0;
    }

    :host(.expandable) .tiptap-editor-wrapper {
      flex: 1;
      max-height: none;
    }

    :host(.expandable) ::ng-deep .ProseMirror {
      min-height: 100%;
    }

    .toolbar-container {
      position: sticky;
      top: 0;
      z-index: 10;
      border-bottom: 1px solid var(--color-border);
      background: var(--color-surface-subtle);
      border-radius: 6px 6px 0 0;
    }

    .toolbar-row {
      display: flex;
      align-items: center;
      gap: 4px;
      padding: 8px;
      flex-wrap: nowrap;
      overflow: hidden;
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
      background: transparent;
      border: none;
      cursor: pointer;
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

    .toolbar-btn.color-btn {
      position: relative;
      flex-direction: column;
      gap: 1px;
      font-weight: 600;
      font-size: 12px;
    }

    .color-indicator {
      position: absolute;
      bottom: 4px;
      left: 50%;
      transform: translateX(-50%);
      width: 14px;
      height: 3px;
      border-radius: 1px;
    }

    .divider {
      width: 1px;
      height: 20px;
      background: var(--color-border);
      margin: 0 4px;
    }

    .hidden-color-input {
      position: absolute;
      opacity: 0;
      pointer-events: none;
      width: 0;
      height: 0;
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
      pointer-events: none;
      opacity: 0;
      transition: opacity 0.15s, background 0.15s;
    }

    .promote-overlay-btn i {
      font-size: 10px;
    }

    .tiptap-editor-wrapper:hover .promote-overlay-btn,
    .promote-overlay-btn:focus-visible {
      opacity: 1;
      pointer-events: auto;
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
      background: var(--color-todo-solid);
      color: white;
    }

    .status-inprogress {
      background: var(--color-inprogress-solid);
      color: white;
    }

    .status-done {
      background: var(--color-done-solid);
      color: white;
    }

    /* Mobile: always show buttons */
    @media (hover: none) {
      .promote-overlay-btn {
        opacity: 1;
        pointer-events: auto;
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

    /* Headings */
    :host ::ng-deep .ProseMirror h1 {
      font-size: 1.75em;
      font-weight: 700;
      margin: 1em 0 0.5em;
    }

    :host ::ng-deep .ProseMirror h1:first-child {
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

    :host ::ng-deep .ProseMirror h3 {
      font-size: 1.1em;
      font-weight: 600;
      margin: 1em 0 0.5em;
    }

    :host ::ng-deep .ProseMirror h3:first-child {
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

    /* Syntax highlighting */
    :host ::ng-deep .ProseMirror pre .hljs-keyword { color: #c678dd; }
    :host ::ng-deep .ProseMirror pre .hljs-string { color: #98c379; }
    :host ::ng-deep .ProseMirror pre .hljs-number { color: #d19a66; }
    :host ::ng-deep .ProseMirror pre .hljs-comment { color: #5c6370; font-style: italic; }
    :host ::ng-deep .ProseMirror pre .hljs-function { color: #61afef; }
    :host ::ng-deep .ProseMirror pre .hljs-variable { color: #e06c75; }
    :host ::ng-deep .ProseMirror pre .hljs-attr { color: #d19a66; }
    :host ::ng-deep .ProseMirror pre .hljs-tag { color: #e06c75; }

    /* Highlight */
    :host ::ng-deep .ProseMirror mark {
      background: #fef08a;
      padding: 0.1em 0.2em;
      border-radius: 2px;
    }

    /* Horizontal Rule */
    :host ::ng-deep .ProseMirror hr {
      border: none;
      border-top: 2px solid var(--color-border);
      margin: 1.5em 0;
    }

    /* Images */
    :host ::ng-deep .ProseMirror img {
      max-width: 100%;
      height: auto;
      border-radius: 4px;
      margin: 0.5em 0;
    }

    /* Tables */
    :host ::ng-deep .ProseMirror table {
      border-collapse: collapse;
      margin: 1em 0;
      width: 100%;
    }

    :host ::ng-deep .ProseMirror th,
    :host ::ng-deep .ProseMirror td {
      border: 1px solid var(--color-border);
      padding: 0.5em;
      text-align: left;
    }

    :host ::ng-deep .ProseMirror th {
      background: var(--color-surface-hover);
      font-weight: 600;
    }

    /* Text alignment */
    :host ::ng-deep .ProseMirror [style*="text-align: center"] {
      text-align: center;
    }

    :host ::ng-deep .ProseMirror [style*="text-align: right"] {
      text-align: right;
    }

    :host ::ng-deep .ProseMirror [style*="text-align: justify"] {
      text-align: justify;
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
    :host ::ng-deep .ProseMirror h1,
    :host ::ng-deep .ProseMirror h2,
    :host ::ng-deep .ProseMirror h3,
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

    /* Links */
    :host ::ng-deep .ProseMirror a.note-link {
      color: var(--color-accent-solid);
      text-decoration: underline;
      text-underline-offset: 2px;
      cursor: pointer;
      transition: color 0.15s;
    }

    :host ::ng-deep .ProseMirror a.note-link:hover {
      color: var(--color-accent-emphasis);
    }

    /* Toggle Section (Details) */
    :host ::ng-deep .ProseMirror details {
      border: 1px solid var(--color-border);
      border-left: 2px solid var(--color-accent-solid);
      border-radius: 6px;
      padding: 0.75rem;
      margin: 0.5em 0;
      background: var(--color-surface-subtle);
    }

    :host ::ng-deep .ProseMirror details summary {
      cursor: pointer;
      font-weight: 600;
      user-select: none;
      list-style: none;
      padding: 0.25em 0;
    }

    :host ::ng-deep .ProseMirror details summary::before {
      content: "▶ ";
      font-size: 0.75em;
      transition: transform 0.15s;
      display: inline-block;
    }

    :host ::ng-deep .ProseMirror details.is-open summary::before {
      content: "▼ ";
    }

    :host ::ng-deep .ProseMirror details.is-open summary {
      margin-bottom: 0.5em;
    }

    :host ::ng-deep .ProseMirror details > div[data-type="detailsContent"] {
      padding-left: 0.25em;
    }

    /* Block type dropdown styling */
    :host ::ng-deep .block-type-dropdown {
      font-size: 13px;
    }

    :host ::ng-deep .block-type-dropdown .p-select-label {
      padding: 4px 8px;
    }

    :host ::ng-deep .block-type-dropdown .p-select-overlay {
      min-width: 160px;
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

  /** Whether the editor should expand to fill available space */
  readonly expandable = input<boolean>(false);

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

  /** Reference to the hidden color input */
  private readonly colorInput = viewChild<ElementRef>('colorInput');

  /** Reference to the toolbar container for ResizeObserver */
  private readonly toolbarContainer = viewChild<ElementRef>('toolbarContainer');

  /** Block type options for the dropdown */
  readonly blockTypes: BlockType[] = [
    { label: 'Paragraph', value: 'paragraph' },
    { label: 'Heading 1', value: 'heading1', style: 'font-size: 1.25em; font-weight: 700;' },
    { label: 'Heading 2', value: 'heading2', style: 'font-size: 1.1em; font-weight: 600;' },
    { label: 'Heading 3', value: 'heading3', style: 'font-size: 1em; font-weight: 600;' },
  ];

  /** Toolbar collapse level: 0 = all visible, higher = more collapsed */
  readonly toolbarLevel = signal(0);

  /** Dynamic overflow menu items based on toolbar level */
  readonly dynamicOverflowMenuItems = computed(() => {
    const level = this.toolbarLevel();
    const items: MenuItem[] = [];

    // Add collapsed groups to overflow menu
    if (level >= 1) {
      items.push({
        label: 'Blocks',
        items: [
          { label: 'Quote', icon: 'pi pi-comment', command: () => this.toggleBlockquote() },
          { label: 'Code Block', icon: 'pi pi-code', command: () => this.toggleCodeBlock() },
        ],
      });
    }
    if (level >= 2) {
      items.push({
        label: 'Alignment',
        items: [
          { label: 'Align Left', icon: 'pi pi-align-left', command: () => this.setTextAlign('left') },
          { label: 'Align Center', icon: 'pi pi-align-center', command: () => this.setTextAlign('center') },
          { label: 'Align Right', icon: 'pi pi-align-right', command: () => this.setTextAlign('right') },
        ],
      });
    }
    if (level >= 3) {
      items.push({
        label: 'Marks',
        items: [
          { label: 'Link', icon: 'pi pi-link', command: () => this.toggleLink() },
          { label: 'Highlight', icon: 'pi pi-sun', command: () => this.toggleHighlight() },
          { label: 'Text Color', icon: 'pi pi-palette', command: () => this.showColorPicker() },
        ],
      });
    }
    if (level >= 4) {
      items.push({
        label: 'Lists',
        items: [
          { label: 'Bullet List', icon: 'pi pi-list', command: () => this.toggleBulletList() },
          { label: 'Numbered List', icon: 'pi pi-sort-numeric-down', command: () => this.toggleOrderedList() },
          { label: 'Task List', icon: 'pi pi-check-square', command: () => this.toggleTaskList() },
        ],
      });
    }

    // Always include the base overflow items
    items.push(
      {
        label: 'Formatting',
        items: [
          { label: 'Inline Code', icon: 'pi pi-code', command: () => this.toggleInlineCode() },
          { label: 'Clear Formatting', icon: 'pi pi-eraser', command: () => this.clearFormatting() },
        ],
      },
      {
        label: 'Insert',
        items: [
          { label: 'Toggle Section', icon: 'pi pi-chevron-down', command: () => this.insertToggleSection() },
          { label: 'Horizontal Rule', icon: 'pi pi-minus', command: () => this.insertHorizontalRule() },
          { label: 'Image', icon: 'pi pi-image', command: () => this.insertImage() },
          { label: 'Table', icon: 'pi pi-table', command: () => this.insertTable() },
        ],
      },
    );

    return items;
  });

  /** Current text color */
  readonly currentTextColor = signal('#000000');

  /** Computed overlay items for promote buttons and status badges */
  readonly checkboxOverlayItems = signal<Array<{
    index: number;
    top: number;
    isLinked: boolean;
    taskStatus: 'Todo' | 'InProgress' | 'Done' | null;
  }>>([]);

  /** Signal to track editor selection changes for reactive updates */
  private readonly selectionVersion = signal(0);

  /** Current block type based on cursor position - reactive via selectionVersion */
  readonly currentBlockType = computed(() => {
    // Read selectionVersion to make this computed reactive on selection changes
    this.selectionVersion();
    if (this.editor.isActive('heading', { level: 1 })) return 'heading1';
    if (this.editor.isActive('heading', { level: 2 })) return 'heading2';
    if (this.editor.isActive('heading', { level: 3 })) return 'heading3';
    // Lists, blockquote, and codeBlock are no longer in dropdown, return paragraph
    return 'paragraph';
  });

  /** Track if we're initializing to avoid emitting on load */
  private isInitializing = true;
  private hasInitialized = false;

  /** Pending setTimeout ID for coalescing button injection calls */
  private pendingButtonInjection: ReturnType<typeof setTimeout> | null = null;

  /** MutationObserver to update overlay when TipTap re-renders */
  private mutationObserver: MutationObserver | null = null;

  /** Scroll handler reference for cleanup */
  private scrollHandler: (() => void) | null = null;

  /** ResizeObserver for toolbar responsiveness */
  private toolbarResizeObserver: ResizeObserver | null = null;

  editor = new Editor({
    editable: true,
    extensions: [
      ...tiptapExtensions,
      Placeholder.configure({
        placeholder: 'Take a note...',
      }),
    ],
    onCreate: () => {
      this.scheduleOverlayUpdate();
    },
    onUpdate: ({ editor }) => {
      if (!this.isInitializing) {
        const json = editor.getJSON();
        this.contentChange.emit(JSON.stringify(json));
      }
      this.scheduleOverlayUpdate();
    },
    onSelectionUpdate: () => {
      // Increment selectionVersion to trigger reactive updates for currentBlockType
      this.selectionVersion.update(v => v + 1);
      this.cdr.markForCheck();
    },
  });

  constructor() {
    effect(() => {
      const content = this.initialContent();
      const isNew = this.isNewNote();
      this.resetTrigger();
      if (this.hasInitialized) {
        this.setEditorContent(content, isNew);
      }
    });

    effect(() => {
      this.checkboxStatuses();
      if (this.hasInitialized) {
        this.scheduleOverlayUpdate();
      }
    });
  }

  ngOnInit(): void {
    this.setEditorContent(this.initialContent(), this.isNewNote());
    this.hasInitialized = true;
  }

  ngAfterViewInit(): void {
    this.setupMutationObserver();
    this.setupScrollListener();
    this.setupToolbarResizeObserver();
    this.scheduleOverlayUpdate();
  }

  private setupToolbarResizeObserver(): void {
    const toolbar = this.toolbarContainer()?.nativeElement as HTMLElement;
    if (!toolbar) return;

    this.toolbarResizeObserver = new ResizeObserver((entries) => {
      const width = entries[0]?.contentRect.width ?? 0;
      // Thresholds determined by group widths:
      // Full toolbar ~630px, collapse progressively as width decreases
      let level: number;
      if (width >= 630) level = 0;       // All visible
      else if (width >= 540) level = 1;  // Hide quote/code
      else if (width >= 440) level = 2;  // Also hide alignment
      else if (width >= 340) level = 3;  // Also hide link/highlight/color
      else level = 4;                    // Also hide lists

      if (this.toolbarLevel() !== level) {
        this.toolbarLevel.set(level);
        this.cdr.markForCheck();
      }
    });

    this.toolbarResizeObserver.observe(toolbar);
  }

  private setupScrollListener(): void {
    const wrapper = this.editorWrapper()?.nativeElement as HTMLElement;
    if (!wrapper) return;

    this.scrollHandler = () => this.scheduleOverlayUpdate();
    wrapper.addEventListener('scroll', this.scrollHandler, { passive: true });
  }

  private setupMutationObserver(): void {
    const container = this.elementRef.nativeElement as HTMLElement;
    const proseMirror = container.querySelector('.ProseMirror');
    if (!proseMirror) return;

    this.mutationObserver = new MutationObserver((mutations) => {
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

  private scheduleOverlayUpdate(): void {
    if (this.pendingButtonInjection !== null) {
      clearTimeout(this.pendingButtonInjection);
    }
    this.pendingButtonInjection = setTimeout(() => {
      this.pendingButtonInjection = null;
      this.updateCheckboxOverlay();
    }, 50);
  }

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

    const statusMap = new Map(
      this.checkboxStatuses().map(s => [s.checkboxId, s])
    );

    const wrapperRect = wrapper.getBoundingClientRect();
    const scrollTop = wrapper.scrollTop;
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
        top: itemRect.top - wrapperRect.top + scrollTop + VERTICAL_ALIGNMENT_OFFSET,
        isLinked: status?.isLinked ?? false,
        taskStatus: status?.taskStatus ?? null,
      });
    });

    this.checkboxOverlayItems.set(items);
    this.cdr.markForCheck();
  }

  onPromoteClick(checkboxIndex: number): void {
    this.promoteCheckbox.emit({ checkboxIndex });
  }

  private setEditorContent(content: string, isNewNote: boolean): void {
    // Skip if the editor already has this content (avoids cursor displacement)
    if (content && this.editor.isFocused) {
      const currentJson = JSON.stringify(this.editor.getJSON());
      if (currentJson === content) {
        return;
      }
    }

    this.isInitializing = true;

    if (content) {
      try {
        const parsed = JSON.parse(content);
        this.editor.commands.setContent(parsed);
      } catch {
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
      this.editor.commands.clearContent();
      this.editor.commands.setHeading({ level: 1 });
    } else {
      this.editor.commands.clearContent();
    }

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
    if (this.toolbarResizeObserver) {
      this.toolbarResizeObserver.disconnect();
    }
    this.editor.destroy();
  }

  // Block type dropdown handler - uses set commands (not toggle) for predictable behavior
  onBlockTypeChange(value: string): void {
    const chain = this.editor.chain().focus();

    switch (value) {
      case 'paragraph':
        chain.setParagraph().run();
        break;
      case 'heading1':
        chain.setHeading({ level: 1 }).run();
        break;
      case 'heading2':
        chain.setHeading({ level: 2 }).run();
        break;
      case 'heading3':
        chain.setHeading({ level: 3 }).run();
        break;
    }
  }

  // Toolbar actions
  toggleBold(): void {
    this.editor.chain().focus().toggleBold().run();
  }

  toggleItalic(): void {
    this.editor.chain().focus().toggleItalic().run();
  }

  toggleUnderline(): void {
    this.editor.chain().focus().toggleUnderline().run();
  }

  toggleStrike(): void {
    this.editor.chain().focus().toggleStrike().run();
  }

  toggleLink(): void {
    if (this.editor.isActive('link')) {
      this.editor.chain().focus().unsetLink().run();
      return;
    }

    // Save selection before window.prompt() steals focus
    const { from, to } = this.editor.state.selection;
    const hasSelection = from !== to;

    if (!hasSelection) {
      const rawUrl = window.prompt('Enter the URL:');
      if (!rawUrl) return;

      const url = this.normalizeUrl(rawUrl);
      if (!url) {
        window.alert('Please enter a valid http, https, or mailto URL.');
        return;
      }

      const text = window.prompt('Enter the link text:', url);
      if (!text) return;

      this.editor
        .chain()
        .focus()
        .setTextSelection(from)
        .insertContent({
          type: 'text',
          marks: [{ type: 'link', attrs: { href: url } }],
          text: text,
        })
        .run();
    } else {
      const rawUrl = window.prompt('Enter the URL:');
      if (!rawUrl) return;

      const url = this.normalizeUrl(rawUrl);
      if (!url) {
        window.alert('Please enter a valid http, https, or mailto URL.');
        return;
      }

      this.editor
        .chain()
        .focus()
        .setTextSelection({ from, to })
        .setLink({ href: url })
        .run();
    }
  }

  private normalizeUrl(input: string): string | null {
    const trimmed = input.trim();
    if (!trimmed) return null;

    const allowedProtocols = ['http:', 'https:', 'mailto:'];

    try {
      const hasProtocol = /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(trimmed);
      const candidate = hasProtocol ? trimmed : `https://${trimmed}`;
      const url = new URL(candidate);

      if (!allowedProtocols.includes(url.protocol)) {
        return null;
      }

      return url.toString();
    } catch {
      return null;
    }
  }

  toggleHighlight(): void {
    this.editor.chain().focus().toggleHighlight().run();
  }

  showColorPicker(): void {
    const input = this.colorInput()?.nativeElement as HTMLInputElement;
    input?.click();
  }

  onColorChange(event: Event): void {
    const color = (event.target as HTMLInputElement).value;
    this.currentTextColor.set(color);
    this.editor.chain().focus().setColor(color).run();
  }

  setTextAlign(alignment: 'left' | 'center' | 'right' | 'justify'): void {
    this.editor.chain().focus().setTextAlign(alignment).run();
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

  toggleHeading(level: 1 | 2 | 3 = 2): void {
    this.editor.chain().focus().toggleHeading({ level }).run();
  }

  toggleBlockquote(): void {
    this.editor.chain().focus().toggleBlockquote().run();
  }

  toggleCodeBlock(): void {
    this.editor.chain().focus().toggleCodeBlock().run();
  }

  toggleInlineCode(): void {
    this.editor.chain().focus().toggleCode().run();
  }

  clearFormatting(): void {
    this.editor.chain().focus().clearNodes().unsetAllMarks().run();
  }

  insertHorizontalRule(): void {
    this.editor.chain().focus().setHorizontalRule().run();
  }

  insertImage(): void {
    const rawUrl = window.prompt('Enter the image URL:');
    if (!rawUrl) return;

    const url = this.normalizeUrl(rawUrl);
    if (!url) {
      window.alert('Please enter a valid http or https URL.');
      return;
    }

    this.editor.chain().focus().setImage({ src: url }).run();
  }

  insertToggleSection(): void {
    this.editor.chain().focus().setDetails().run();
  }

  insertTable(): void {
    this.editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run();
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
