import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Header } from '../../core/components/header/header';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, Header],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing {}
