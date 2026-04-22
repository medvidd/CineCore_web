import { Component } from '@angular/core';
import { Header } from '../../core/components/header/header';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [Header],
  templateUrl: './account.html',
  styleUrl: './account.scss'
})
export class Account { }
