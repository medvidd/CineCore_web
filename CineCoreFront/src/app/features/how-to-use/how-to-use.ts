import { Component, inject, ChangeDetectorRef } from '@angular/core'; // ДОДАНО ChangeDetectorRef
import { RouterLink } from '@angular/router';
import { Header } from '../../core/components/header/header';
import { Api } from '../../core/services/api';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-how-to-use',
  standalone: true,
  imports: [RouterLink, Header, CommonModule],
  templateUrl: './how-to-use.html',
  styleUrl: './how-to-use.scss',
})
export class HowToUse {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef); // ІНЖЕКТИМО СЕРВІС ОНОВЛЕННЯ ЕКРАНУ

  seedResult: any = null;

  isSeedModalOpen = false;
  isImporting = false;
  isFinished = false;

  progress = 0;
  currentStatus = 'Ready to start...';
  processedRows = 0;
  totalRows = 1000;
  currentBatch = 0;
  totalBatches = 10;

  private progressInterval: any;

  openSeedModal() {
    this.isSeedModalOpen = true;
    this.resetState();
  }

  closeSeedModal() {
    if (!this.isImporting) {
      this.isSeedModalOpen = false;
    }
  }

  resetState() {
    this.isImporting = false;
    this.isFinished = false;
    this.seedResult = null;
    this.progress = 0;
    this.currentStatus = 'Ready to start...';
    this.processedRows = 0;
    this.currentBatch = 0;
  }

  startImport() {
    this.isImporting = true;
    this.currentStatus = 'Reading CSV file and extracting data...';

    this.simulateProgress();

    this.api.runSeedDatabase().subscribe({
      next: (res: any) => {
        clearInterval(this.progressInterval);

        this.progress = 100;
        this.processedRows = this.totalRows;
        this.currentBatch = this.totalBatches;
        this.currentStatus = 'Complete!';

        this.seedResult = res;
        this.isImporting = false;
        this.isFinished = true;

        this.cdr.detectChanges(); // ПРИМУСОВЕ ОНОВЛЕННЯ ПРИ ЗАВЕРШЕННІ
      },
      error: (err) => {
        clearInterval(this.progressInterval);
        this.isImporting = false;
        this.currentStatus = 'Error occurred!';
        alert(err.error?.message || 'Помилка під час ініціалізації бази даних.');
        this.cdr.detectChanges(); // ПРИМУСОВЕ ОНОВЛЕННЯ ПРИ ПОМИЛЦІ
      }
    });
  }

  private simulateProgress() {
    this.progressInterval = setInterval(() => {
      if (this.progress < 90) {
        const step = Math.floor(Math.random() * 5) + 2;
        this.progress += step;
        if (this.progress > 90) this.progress = 90;

        this.processedRows = Math.floor((this.progress / 100) * this.totalRows);
        this.currentBatch = Math.ceil((this.progress / 100) * this.totalBatches);

        if (this.progress > 20 && this.progress < 50) {
          this.currentStatus = 'Creating User accounts...';
        } else if (this.progress >= 50 && this.progress < 80) {
          this.currentStatus = 'Generating Projects & Roles...';
        } else if (this.progress >= 80) {
          this.currentStatus = 'Mapping Crew relations...';
        }

        this.cdr.detectChanges(); // СЕКРЕТНИЙ ІНГРЕДІЄНТ: ПРИМУШУЄМО UI РУХАТИСЯ
      }
    }, 400);
  }
}
