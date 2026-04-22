import { Component } from '@angular/core';
import { Header } from '../../../core/components/header/header';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [Header, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {}
