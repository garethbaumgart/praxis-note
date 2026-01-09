import { Directive, ElementRef, inject, AfterViewInit } from '@angular/core';

@Directive({
  selector: 'textarea[appAutoResize]',
  standalone: true,
  host: {
    '(input)': 'resize()',
  },
})
export class AutoResizeDirective implements AfterViewInit {
  private readonly el = inject(ElementRef<HTMLTextAreaElement>);

  ngAfterViewInit(): void {
    this.resize();
  }

  resize(): void {
    const textarea = this.el.nativeElement;
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
  }
}
