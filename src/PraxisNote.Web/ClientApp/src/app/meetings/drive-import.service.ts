import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {
  DriveFileImportDto,
  DriveImportPreviewFile,
  DriveImportConfirmResult,
  ParsedResult,
} from './drive-import.model';

@Injectable({ providedIn: 'root' })
export class DriveImportService {
  private readonly http = inject(HttpClient);

  readonly state = signal<'idle' | 'loading' | 'preview' | 'importing' | 'done' | 'error'>('idle');
  readonly files = signal<DriveImportPreviewFile[]>([]);
  readonly error = signal<string | null>(null);
  readonly importResult = signal<DriveImportConfirmResult | null>(null);
  readonly importProgress = signal<{ current: number; total: number }>({ current: 0, total: 0 });

  readonly selectedCount = computed(() =>
    this.files().filter(f => f.selected && f.duplicateType !== 'definite').length,
  );
  readonly duplicateCount = computed(() =>
    this.files().filter(f => f.duplicateType === 'definite').length,
  );
  readonly possibleDuplicateCount = computed(() =>
    this.files().filter(f => f.duplicateType === 'possible').length,
  );
  readonly totalCount = computed(() => this.files().length);

  loadPreview(): void {
    this.state.set('loading');
    this.error.set(null);

    this.http.get<DriveFileImportDto[]>('/api/drive/files?status=Parsed').subscribe({
      next: (dtos) => {
        const previewFiles = dtos.map(dto => this.mapToPreviewFile(dto));
        this.files.set(previewFiles);
        this.state.set('preview');
      },
      error: () => {
        this.error.set('Failed to load Drive files for import.');
        this.state.set('error');
      },
    });
  }

  toggleFile(index: number): void {
    this.files.update(files => {
      const updated = [...files];
      const file = updated[index];
      if (file.duplicateType === 'definite') return files;
      updated[index] = { ...file, selected: !file.selected };
      return updated;
    });
  }

  toggleAll(selected: boolean): void {
    this.files.update(files =>
      files.map(f =>
        f.duplicateType === 'definite' ? f : { ...f, selected },
      ),
    );
  }

  toggleExpanded(index: number): void {
    this.files.update(files => {
      const updated = [...files];
      updated[index] = { ...updated[index], expanded: !updated[index].expanded };
      return updated;
    });
  }

  removeTag(fileIndex: number, tagName: string): void {
    this.files.update(files => {
      const updated = [...files];
      const file = updated[fileIndex];
      updated[fileIndex] = {
        ...file,
        editedTags: file.editedTags.filter(t => t !== tagName),
      };
      return updated;
    });
  }

  addTag(fileIndex: number, tagName: string): void {
    if (!tagName.trim()) return;
    this.files.update(files => {
      const updated = [...files];
      const file = updated[fileIndex];
      if (file.editedTags.some(t => t.toLowerCase() === tagName.toLowerCase())) {
        return files;
      }
      updated[fileIndex] = {
        ...file,
        editedTags: [...file.editedTags, tagName.trim()],
      };
      return updated;
    });
  }

  async confirmImport(): Promise<void> {
    const selectedFiles = this.files().filter(f => f.selected && f.duplicateType !== 'definite');
    const unselectedNonDuplicates = this.files().filter(
      f => !f.selected && f.duplicateType !== 'definite',
    );

    if (selectedFiles.length === 0) return;

    this.state.set('importing');
    this.importProgress.set({ current: 0, total: selectedFiles.length });

    try {
      // Skip unselected non-duplicate files
      if (unselectedNonDuplicates.length > 0) {
        await this.skipFiles(unselectedNonDuplicates.map(f => f.id));
      }

      // Confirm selected files
      const body = {
        files: selectedFiles.map(f => ({
          driveFileImportId: f.id,
          tags: f.editedTags,
        })),
      };

      this.importProgress.set({ current: 1, total: selectedFiles.length });

      const result = await new Promise<DriveImportConfirmResult>((resolve, reject) => {
        this.http.post<DriveImportConfirmResult>('/api/drive/import/confirm', body).subscribe({
          next: (res) => resolve(res),
          error: (err) => reject(err),
        });
      });

      this.importProgress.set({ current: selectedFiles.length, total: selectedFiles.length });
      this.importResult.set(result);
      this.state.set('done');
    } catch {
      this.error.set('Failed to import meetings. Please try again.');
      this.state.set('error');
    }
  }

  async retryFailed(): Promise<void> {
    const result = this.importResult();
    if (!result?.failures?.length) return;

    const failedIds = result.failures.map(f => f.driveFileImportId);
    const failedFiles = this.files().filter(f => failedIds.includes(f.id));

    if (failedFiles.length === 0) return;

    this.state.set('importing');
    this.importProgress.set({ current: 0, total: failedFiles.length });

    try {
      const body = {
        files: failedFiles.map(f => ({
          driveFileImportId: f.id,
          tags: f.editedTags,
        })),
      };

      this.importProgress.set({ current: 1, total: failedFiles.length });

      const retryResult = await new Promise<DriveImportConfirmResult>((resolve, reject) => {
        this.http.post<DriveImportConfirmResult>('/api/drive/import/confirm', body).subscribe({
          next: (res) => resolve(res),
          error: (err) => reject(err),
        });
      });

      this.importProgress.set({ current: failedFiles.length, total: failedFiles.length });

      // Merge with previous result
      const prev = this.importResult()!;
      this.importResult.set({
        importedCount: prev.importedCount + retryResult.importedCount,
        totalActionItems: prev.totalActionItems + retryResult.totalActionItems,
        tagsCreated: prev.tagsCreated + retryResult.tagsCreated,
        skippedCount: prev.skippedCount + retryResult.skippedCount,
        failures: retryResult.failures,
      });
      this.state.set('done');
    } catch {
      this.error.set('Retry failed. Please try again.');
      this.state.set('error');
    }
  }

  reset(): void {
    this.state.set('idle');
    this.files.set([]);
    this.error.set(null);
    this.importResult.set(null);
    this.importProgress.set({ current: 0, total: 0 });
  }

  private async skipFiles(ids: string[]): Promise<void> {
    await new Promise<void>((resolve, reject) => {
      this.http.post('/api/drive/import/skip', { driveFileImportIds: ids }).subscribe({
        next: () => resolve(),
        error: (err) => reject(err),
      });
    });
  }

  private mapToPreviewFile(dto: DriveFileImportDto): DriveImportPreviewFile {
    let parsed: ParsedResult | null = null;
    if (dto.parsedResultJson) {
      try {
        parsed = JSON.parse(dto.parsedResultJson);
      } catch {
        // Ignore parse errors
      }
    }

    const duplicateType = this.mapDuplicateType(dto.duplicateType);

    return {
      id: dto.id,
      driveFileId: dto.driveFileId,
      fileName: dto.fileName,
      title: parsed?.title ?? null,
      meetingDate: parsed?.meetingDate ?? null,
      attendees: parsed?.attendees ?? null,
      summary: parsed?.summary ?? null,
      keyPoints: parsed?.keyPoints ?? null,
      decisions: parsed?.decisions ?? null,
      actionItems: parsed?.actionItems ?? null,
      suggestedTags: parsed?.suggestedTags ?? [],
      duplicateType,
      duplicateConfidence: dto.duplicateConfidence,
      matchedMeetingId: dto.matchedMeetingId,
      matchedMeetingTitle: dto.duplicateMatchTitle,
      status: dto.status,
      selected: duplicateType !== 'definite',
      expanded: false,
      editedTags: [...(parsed?.suggestedTags ?? [])],
    };
  }

  private mapDuplicateType(type: string): 'none' | 'definite' | 'possible' {
    switch (type) {
      case 'ExactFile':
      case 'CalendarEvent':
        return 'definite';
      case 'FuzzyMatch':
        return 'possible';
      default:
        return 'none';
    }
  }
}
