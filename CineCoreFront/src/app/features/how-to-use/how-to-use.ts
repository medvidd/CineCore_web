import { Component } from '@angular/core';
import {RouterLink, RouterLinkActive} from '@angular/router';
import { Header } from '../../core/components/header/header';

@Component({
  selector: 'app-how-to-use',
  standalone: true,
  imports: [RouterLink, Header],
  templateUrl: './how-to-use.html',
  styleUrl: './how-to-use.scss',
})
export class HowToUse {}
