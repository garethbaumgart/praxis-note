export interface ExtractedCalendarEvent {
  title: string;
  startTime: string;
  endTime: string;
  attendees: string | null;
  location: string | null;
  selected: boolean;
}

export interface ScreenshotExtractionResult {
  events: Omit<ExtractedCalendarEvent, 'selected'>[];
}
