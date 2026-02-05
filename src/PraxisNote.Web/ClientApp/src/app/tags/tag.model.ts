/** Full tag with usage statistics for listing. */
export interface Tag {
  id: string;
  name: string;
  usageCount: number;
}

/** Minimal tag info for embedding in task responses. */
export interface TaskTag {
  id: string;
  name: string;
}
