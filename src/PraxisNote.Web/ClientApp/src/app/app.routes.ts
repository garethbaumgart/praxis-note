import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  {
    path: 'home',
    loadComponent: () => import('./home/home.page').then(m => m.HomePage),
  },
  {
    path: 'tasks',
    loadComponent: () => import('./tasks/tasks.page').then(m => m.TasksPage),
  },
  {
    path: 'notes',
    loadComponent: () => import('./notes/notes.page').then(m => m.NotesPage),
  },
  {
    // This route matches both /notes/{actual-id} and /notes/new.
    // NoteEditorPage treats 'new' specially to create a new note.
    // If adding routes like /notes/archived, define them above this route.
    path: 'notes/:id',
    loadComponent: () => import('./notes/note-editor.page').then(m => m.NoteEditorPage),
  },
  {
    path: 'meetings',
    loadComponent: () => import('./meetings/meetings.page').then(m => m.MeetingsPage),
  },
  {
    path: 'meetings/:id',
    loadComponent: () => import('./meetings/meeting-editor.page').then(m => m.MeetingEditorPage),
  },
  {
    path: 'settings',
    loadComponent: () => import('./settings/settings.page').then(m => m.SettingsPage),
  },
];
