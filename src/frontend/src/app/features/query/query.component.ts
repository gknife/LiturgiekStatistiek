import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-query',
  standalone: true,
  imports: [MatCardModule, MatTabsModule],
  templateUrl: './query.component.html',
  styleUrl: './query.component.scss',
})
export class QueryComponent {}
