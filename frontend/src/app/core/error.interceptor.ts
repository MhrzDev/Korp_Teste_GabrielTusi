import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from './notification.service';

export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifications = inject(NotificationService);

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      const message = error.error?.detail ?? error.error?.message ??
        'Não foi possível concluir a operação. Tente novamente.';
      notifications.show(message, 'error');
      return throwError(() => error);
    })
  );
};

