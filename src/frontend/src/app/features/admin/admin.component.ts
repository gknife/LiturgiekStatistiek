import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [
    CommonModule, MatCardModule, MatTabsModule, MatIconModule,
    MatButtonModule, MatInputModule, MatFormFieldModule, FormsModule,
  ],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  readonly editTitle = signal('');
  readonly editContent = signal('');
  readonly saving = signal(false);
  readonly savedMessage = signal('');

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getContent('homepage').subscribe({
      next: page => {
        this.editTitle.set(page.titleNl);
        this.editContent.set(page.contentMarkdown);
      },
    });
  }

  saveContent(): void {
    this.saving.set(true);
    this.api.updateContent('homepage', {
      titleNl: this.editTitle(),
      contentMarkdown: this.editContent(),
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.savedMessage.set('Opgeslagen!');
        setTimeout(() => this.savedMessage.set(''), 3000);
      },
      error: () => { this.saving.set(false); },
    });
  }
}
