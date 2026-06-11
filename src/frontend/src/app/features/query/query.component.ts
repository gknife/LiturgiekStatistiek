import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-query',
  standalone: true,
  imports: [
    MatCardModule, MatTabsModule, MatIconModule, MatButtonModule,
    MatInputModule, MatFormFieldModule, MatSelectModule, FormsModule,
  ],
  templateUrl: './query.component.html',
  styleUrl: './query.component.scss',
})
export class QueryComponent {
  naturalLanguageQuery = '';

  queryTemplates = [
    { id: 'most-sung', title: 'Meest gezongen lied', description: 'Welk lied wordt het meest gezongen in een gemeente?', icon: 'music_note' },
    { id: 'most-verse', title: 'Meest gezongen couplet', description: 'Welk couplet van een lied wordt het vaakst gezongen?', icon: 'format_list_numbered' },
    { id: 'opening-song', title: 'Meest gezongen openingslied', description: 'Welk lied wordt het vaakst als openingslied gebruikt?', icon: 'play_arrow' },
    { id: 'avg-songs', title: 'Gemiddeld aantal liederen', description: 'Hoeveel liederen/coupletten worden gemiddeld gezongen?', icon: 'calculate' },
    { id: 'psalm-compare', title: 'Psalmengebruik vergelijken', description: 'Welke gemeente zingt de meeste psalmen? Vergelijking.', icon: 'compare_arrows' },
    { id: 'city-map', title: 'Lied per stad (kaart)', description: 'Welke stad zingt een bepaald lied het meest?', icon: 'map' },
    { id: 'seasonal', title: 'Liederen per seizoen', description: 'Welke liederen worden in een bepaalde periode gezongen?', icon: 'calendar_month' },
    { id: 'song-lookup', title: 'Diensten met lied X', description: 'Geef alle diensten waar een bepaald lied wordt gezongen.', icon: 'search' },
    { id: 'sequence', title: 'Lied X na lied Y', description: 'Welke diensten hebben lied X direct na lied Y?', icon: 'swap_vert' },
    { id: 'trend', title: 'Gebruik over tijd', description: 'Hoe is het gebruik van een lied toe-/afgenomen?', icon: 'trending_up' },
  ];

  exampleQueries = [
    'Welk lied wordt het meest gezongen in de GG?',
    'Welk couplet van Psalm 119 wordt het vaakst gezongen?',
    'Vergelijk psalmengebruik tussen PKN en NGK',
    'Welke stad zingt Psalm 150 het meest?',
    'Geef alle diensten waar Psalm 6 gezongen wordt',
    'Hoe is het gebruik van Psalm 116 veranderd over de jaren?',
  ];

  submitNaturalLanguageQuery(): void {
    // TODO: Send to backend LLM endpoint
    console.log('Query:', this.naturalLanguageQuery);
  }
}
