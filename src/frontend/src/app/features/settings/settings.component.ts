import { Component, computed, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { FontSize, ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    MatCardModule, MatSlideToggleModule, MatRadioModule,
    MatSelectModule, MatFormFieldModule, MatIconModule, FormsModule,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  private readonly theme = inject(ThemeService);

  readonly preferences = this.theme.preferences;
  readonly isDark = computed(() => this.preferences().theme === 'dark');

  readonly accentColors = [
    { value: '#2c5282', label: 'Blauw (standaard)' },
    { value: '#1a365d', label: 'Donkerblauw' },
    { value: '#2f855a', label: 'Groen' },
    { value: '#744210', label: 'Bruin' },
    { value: '#553c9a', label: 'Paars' },
  ];

  onThemeToggle(isDark: boolean): void {
    this.theme.setTheme(isDark ? 'dark' : 'light');
  }

  onFontSizeChange(fontSize: FontSize): void {
    this.theme.setFontSize(fontSize);
  }

  onAccentChange(accentColor: string): void {
    this.theme.setAccentColor(accentColor);
  }
}
