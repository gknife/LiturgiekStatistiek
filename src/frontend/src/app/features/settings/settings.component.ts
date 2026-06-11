import { Component, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatRadioModule } from '@angular/material/radio';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';

interface UserPreferences {
  theme: 'light' | 'dark';
  fontSize: 'small' | 'medium' | 'large';
  accentColor: string;
}

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
export class SettingsComponent implements OnInit {
  preferences: UserPreferences = {
    theme: 'light',
    fontSize: 'medium',
    accentColor: '#2c5282',
  };

  accentColors = [
    { value: '#2c5282', label: 'Blauw (standaard)' },
    { value: '#1a365d', label: 'Donkerblauw' },
    { value: '#2f855a', label: 'Groen' },
    { value: '#744210', label: 'Bruin' },
    { value: '#553c9a', label: 'Paars' },
  ];

  ngOnInit(): void {
    const saved = localStorage.getItem('liturgiek-preferences');
    if (saved) {
      this.preferences = JSON.parse(saved);
      this.applyTheme();
    }
  }

  onThemeToggle(isDark: boolean): void {
    this.preferences.theme = isDark ? 'dark' : 'light';
    this.applyTheme();
    this.save();
  }

  onFontSizeChange(): void {
    this.applyFontSize();
    this.save();
  }

  onAccentChange(): void {
    this.save();
  }

  private applyTheme(): void {
    document.body.style.colorScheme = this.preferences.theme;
  }

  private applyFontSize(): void {
    const sizes = { small: '14px', medium: '16px', large: '18px' };
    document.documentElement.style.fontSize = sizes[this.preferences.fontSize];
  }

  private save(): void {
    localStorage.setItem('liturgiek-preferences', JSON.stringify(this.preferences));
  }
}
