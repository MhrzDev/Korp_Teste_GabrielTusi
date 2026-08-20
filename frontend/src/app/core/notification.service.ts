import { Injectable, signal } from '@angular/core';

export interface Notification {
  message: string;
  type: 'success' | 'error';
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  readonly current = signal<Notification | null>(null);
  private timer?: ReturnType<typeof setTimeout>;

  show(message: string, type: Notification['type']): void {
    clearTimeout(this.timer);
    this.current.set({ message, type });
    this.timer = setTimeout(() => this.current.set(null), 5000);
  }

  clear(): void {
    this.current.set(null);
  }
}

