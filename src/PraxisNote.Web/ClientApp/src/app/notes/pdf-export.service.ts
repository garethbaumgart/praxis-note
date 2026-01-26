import { Injectable } from '@angular/core';
import { jsPDF } from 'jspdf';
import { Note } from './note.model';

interface TiptapNode {
  type: string;
  content?: TiptapNode[];
  text?: string;
  attrs?: Record<string, unknown>;
  marks?: Array<{ type: string }>;
}

@Injectable({
  providedIn: 'root',
})
export class PdfExportService {
  private readonly pageWidth = 210; // A4 width in mm
  private readonly pageHeight = 297; // A4 height in mm
  private readonly margin = 20;
  private readonly lineHeight = 7;
  private readonly maxWidth = 170; // pageWidth - 2 * margin

  exportNoteToPdf(note: Note): void {
    const doc = new jsPDF();
    let yPosition = this.margin;

    // Parse TipTap content
    let content: TiptapNode | null = null;
    try {
      content = JSON.parse(note.content);
    } catch {
      // Plain text fallback
      content = {
        type: 'doc',
        content: [{ type: 'paragraph', content: [{ type: 'text', text: note.content || 'Empty note' }] }],
      };
    }

    // Render content
    if (content?.type === 'doc' && content.content) {
      yPosition = this.renderNodes(doc, content.content, yPosition, 0);
    }

    // Add checkboxes if present
    if (note.checkboxes.length > 0) {
      yPosition += this.lineHeight;
      yPosition = this.checkPageBreak(doc, yPosition);

      for (const checkbox of note.checkboxes) {
        yPosition = this.checkPageBreak(doc, yPosition);
        const prefix = checkbox.isChecked ? '☑' : '☐';
        const text = `${prefix} ${checkbox.text}`;

        if (checkbox.isChecked) {
          doc.setTextColor(128, 128, 128); // Gray for checked
        } else {
          doc.setTextColor(0, 0, 0);
        }

        doc.setFontSize(11);
        doc.setFont('helvetica', 'normal');
        const lines = doc.splitTextToSize(text, this.maxWidth - 10);
        doc.text(lines, this.margin + 5, yPosition);
        yPosition += lines.length * this.lineHeight;
      }
      doc.setTextColor(0, 0, 0); // Reset color
    }

    // Add tags if present
    if (note.tags.length > 0) {
      yPosition += this.lineHeight;
      yPosition = this.checkPageBreak(doc, yPosition);

      doc.setFontSize(9);
      doc.setTextColor(100, 100, 100);
      const tagsText = 'Tags: ' + note.tags.map((t) => t.name).join(', ');
      doc.text(tagsText, this.margin, yPosition);
      doc.setTextColor(0, 0, 0);
    }

    // Generate filename from first line or note ID
    const filename = this.generateFilename(note);
    doc.save(filename);
  }

  private renderNodes(doc: jsPDF, nodes: TiptapNode[], yPosition: number, indent: number): number {
    for (const node of nodes) {
      yPosition = this.checkPageBreak(doc, yPosition);

      switch (node.type) {
        case 'heading':
          yPosition = this.renderHeading(doc, node, yPosition);
          break;
        case 'paragraph':
          yPosition = this.renderParagraph(doc, node, yPosition, indent);
          break;
        case 'bulletList':
          yPosition = this.renderBulletList(doc, node, yPosition, indent);
          break;
        case 'orderedList':
          yPosition = this.renderOrderedList(doc, node, yPosition, indent);
          break;
        case 'taskList':
          yPosition = this.renderTaskList(doc, node, yPosition, indent);
          break;
        case 'blockquote':
          yPosition = this.renderBlockquote(doc, node, yPosition);
          break;
        case 'codeBlock':
          yPosition = this.renderCodeBlock(doc, node, yPosition);
          break;
        default:
          // For unknown nodes with content, render their children
          if (node.content) {
            yPosition = this.renderNodes(doc, node.content, yPosition, indent);
          }
      }
    }
    return yPosition;
  }

  private renderHeading(doc: jsPDF, node: TiptapNode, yPosition: number): number {
    const level = (node.attrs?.['level'] as number) || 2;
    const fontSize = level === 1 ? 18 : level === 2 ? 16 : 14;

    doc.setFontSize(fontSize);
    doc.setFont('helvetica', 'bold');

    const text = this.extractText(node);
    const lines = doc.splitTextToSize(text, this.maxWidth);
    doc.text(lines, this.margin, yPosition);

    return yPosition + lines.length * (fontSize * 0.4) + 4;
  }

  private renderParagraph(doc: jsPDF, node: TiptapNode, yPosition: number, indent: number): number {
    doc.setFontSize(11);
    doc.setFont('helvetica', 'normal');

    const text = this.extractText(node);
    if (!text.trim()) {
      return yPosition + this.lineHeight / 2; // Empty paragraph = half line
    }

    const effectiveWidth = this.maxWidth - indent * 5;
    const lines = doc.splitTextToSize(text, effectiveWidth);
    doc.text(lines, this.margin + indent * 5, yPosition);

    return yPosition + lines.length * this.lineHeight;
  }

  private renderBulletList(doc: jsPDF, node: TiptapNode, yPosition: number, indent: number): number {
    if (!node.content) return yPosition;

    for (const item of node.content) {
      if (item.type === 'listItem') {
        yPosition = this.checkPageBreak(doc, yPosition);
        doc.setFontSize(11);
        doc.setFont('helvetica', 'normal');

        // Draw bullet
        const bulletX = this.margin + indent * 5 + 2;
        doc.text('•', bulletX, yPosition);

        // Render item content
        if (item.content) {
          yPosition = this.renderNodes(doc, item.content, yPosition, indent + 2);
        }
      }
    }

    return yPosition;
  }

  private renderOrderedList(doc: jsPDF, node: TiptapNode, yPosition: number, indent: number): number {
    if (!node.content) return yPosition;

    let index = 1;
    for (const item of node.content) {
      if (item.type === 'listItem') {
        yPosition = this.checkPageBreak(doc, yPosition);
        doc.setFontSize(11);
        doc.setFont('helvetica', 'normal');

        // Draw number
        const numberX = this.margin + indent * 5;
        doc.text(`${index}.`, numberX, yPosition);

        // Render item content
        if (item.content) {
          yPosition = this.renderNodes(doc, item.content, yPosition, indent + 2);
        }
        index++;
      }
    }

    return yPosition;
  }

  private renderTaskList(doc: jsPDF, node: TiptapNode, yPosition: number, indent: number): number {
    if (!node.content) return yPosition;

    for (const item of node.content) {
      if (item.type === 'taskItem') {
        yPosition = this.checkPageBreak(doc, yPosition);
        const checked = item.attrs?.['checked'] as boolean;
        const checkbox = checked ? '☑' : '☐';

        doc.setFontSize(11);
        if (checked) {
          doc.setTextColor(128, 128, 128);
        }

        const checkboxX = this.margin + indent * 5;
        doc.text(checkbox, checkboxX, yPosition);

        // Render item content
        if (item.content) {
          yPosition = this.renderNodes(doc, item.content, yPosition, indent + 2);
        }

        doc.setTextColor(0, 0, 0);
      }
    }

    return yPosition;
  }

  private renderBlockquote(doc: jsPDF, node: TiptapNode, yPosition: number): number {
    doc.setFontSize(11);
    doc.setFont('helvetica', 'italic');
    doc.setTextColor(80, 80, 80);

    // Draw left border
    const borderX = this.margin + 2;
    doc.setDrawColor(180, 180, 180);
    doc.setLineWidth(0.5);

    const text = this.extractText(node);
    const lines = doc.splitTextToSize(text, this.maxWidth - 15);

    const startY = yPosition - 3;
    const endY = yPosition + lines.length * this.lineHeight;
    doc.line(borderX, startY, borderX, endY);

    doc.text(lines, this.margin + 8, yPosition);

    doc.setFont('helvetica', 'normal');
    doc.setTextColor(0, 0, 0);

    return yPosition + lines.length * this.lineHeight + 2;
  }

  private renderCodeBlock(doc: jsPDF, node: TiptapNode, yPosition: number): number {
    doc.setFontSize(10);
    doc.setFont('courier', 'normal');
    doc.setTextColor(60, 60, 60);

    const text = this.extractText(node);
    const lines = doc.splitTextToSize(text, this.maxWidth - 10);

    // Draw background
    doc.setFillColor(245, 245, 245);
    const boxHeight = lines.length * 6 + 6;
    doc.rect(this.margin, yPosition - 5, this.maxWidth, boxHeight, 'F');

    doc.text(lines, this.margin + 5, yPosition);

    doc.setFont('helvetica', 'normal');
    doc.setTextColor(0, 0, 0);

    return yPosition + boxHeight + 2;
  }

  private extractText(node: TiptapNode): string {
    if (node.type === 'text' && node.text) {
      return node.text;
    }

    if (!node.content) {
      return '';
    }

    return node.content.map((child) => this.extractText(child)).join('');
  }

  private checkPageBreak(doc: jsPDF, yPosition: number): number {
    if (yPosition > this.pageHeight - this.margin) {
      doc.addPage();
      return this.margin;
    }
    return yPosition;
  }

  private generateFilename(note: Note): string {
    // Try to get first line as title
    let title = 'note';

    try {
      const content = JSON.parse(note.content);
      if (content?.type === 'doc' && content.content?.[0]) {
        const firstNode = content.content[0];
        const text = this.extractText(firstNode).trim();
        if (text) {
          // Clean up for filename
          title = text
            .substring(0, 50)
            .replace(/[^a-zA-Z0-9\s-]/g, '')
            .trim()
            .replace(/\s+/g, '-')
            .toLowerCase();
        }
      }
    } catch {
      // Use note ID if parsing fails
      title = `note-${note.id.substring(0, 8)}`;
    }

    return `${title || 'note'}.pdf`;
  }
}
