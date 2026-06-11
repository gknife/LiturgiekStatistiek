import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ContentPage } from '../../core/models/api.models';

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
  homepageContent: ContentPage | null = null;
  editTitle = '';
  editContent = '';
  saving = false;
  savedMessage = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getContent('homepage').subscribe({
      next: page => {
        this.homepageContent = page;
        this.editTitle = page.titleNl;
        this.editContent = page.contentMarkdown;
      },
    });
  }

  saveContent(): void {
    this.saving = true;
    this.api.updateContent('homepage', {
      titleNl: this.editTitle,
      contentMarkdown: this.editContent,
    }).subscribe({
      next: () => {
        this.saving = false;
        this.savedMessage = 'Opgeslagen!';
        setTimeout(() => this.savedMessage = '', 3000);
      },
      error: () => { this.saving = false; },
    });
  }
}
