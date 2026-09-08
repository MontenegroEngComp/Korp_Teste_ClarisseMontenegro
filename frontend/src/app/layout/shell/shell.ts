import {
  Component,
  computed,
  inject,
} from '@angular/core';
import {
  Router,
  RouterLink,
  RouterLinkActive,
  RouterOutlet,
} from '@angular/router';

import { Auth } from '../../core/services/auth';

@Component({
  selector: 'app-shell',
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
  ],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class Shell {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);

  readonly currentEmployee =
    this.auth.currentEmployee;

  readonly employeeInitials = computed(() => {
    const name = this.currentEmployee()?.name;

    if (!name) {
      return 'US';
    }

    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  });

  readonly roleLabel = computed(() =>
    this.currentEmployee()?.role === 'Administrator'
      ? 'Administradora'
      : 'Operadora',
  );

  readonly currentDate = new Intl.DateTimeFormat(
    'pt-BR',
    {
      weekday: 'long',
      day: '2-digit',
      month: 'long',
    },
  )
    .format(new Date())
    .toUpperCase();

  logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}