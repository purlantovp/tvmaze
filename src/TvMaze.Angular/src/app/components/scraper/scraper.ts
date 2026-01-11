import { Component, ChangeDetectorRef } from '@angular/core';
import { TvmazeService } from '../../services/tvmaze';
import { ScrapeResult } from '../../models/scrape-result.model';

@Component({
  selector: 'app-scraper',
  standalone: false,
  templateUrl: './scraper.html',
  styleUrl: './scraper.css',
})
export class Scraper {
  startPage: number = 0;
  pageCount: number = 1;
  loading: boolean = false;
  result: ScrapeResult | null = null;
  error: string = '';

  constructor(
    private tvmazeService: TvmazeService,
    private cdr: ChangeDetectorRef
  ) {}

  startScrape(): void {
    this.loading = true;
    this.result = null;
    this.error = '';

    this.tvmazeService.scrape(this.startPage, this.pageCount).subscribe({
      next: (result: ScrapeResult) => {
        this.result = result;
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Scrape error:', error);
        this.error = 'Failed to start scrape. Please try again.';
        this.loading = false;
        this.cdr.markForCheck();
        this.cdr.detectChanges();
      },
    });
  }
}
