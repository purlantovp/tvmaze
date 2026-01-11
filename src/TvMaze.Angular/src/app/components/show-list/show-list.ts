import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { TvmazeService } from '../../services/tvmaze';
import { PagedResult, Show } from '../../models/show.model';

@Component({
  selector: 'app-show-list',
  standalone: false,
  templateUrl: './show-list.html',
  styleUrl: './show-list.css',
})
export class ShowList implements OnInit {
  shows: Show[] = [];
  pageNumber: number = 1;
  pageSize: number = 10;
  totalPages: number = 0;
  totalCount: number = 0;
  searchTerm: string = '';
  orderBy: string = '';
  loading: boolean = false;

  constructor(private tvmazeService: TvmazeService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadShows();
  }

  loadShows(): void {
    this.loading = true;
    this.tvmazeService
      .getShows(
        this.pageNumber,
        this.pageSize,
        this.orderBy || undefined,
        this.searchTerm || undefined
      )
      .subscribe({
        next: (result: PagedResult<Show>) => {
          this.shows = result.items;
          this.pageNumber = result.pageNumber;
          this.pageSize = result.pageSize;
          this.totalPages = result.totalPages;
          this.totalCount = result.totalCount;
          this.loading = false;
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Error loading shows:', error);
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
  }

  onSearch(): void {
    this.pageNumber = 1; // Reset to first page on search
    this.loadShows();
  }

  onOrderChange(): void {
    this.pageNumber = 1; // Reset to first page on sort
    this.loadShows();
  }

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.pageNumber = newPage;
      this.loadShows();
    }
  }

  viewShowDetails(showId: number): void {
    this.router.navigate(['/show', showId]);
  }

  getPages(): number[] {
    const pages: number[] = [];
    const maxPagesToShow = 5;
    let startPage = Math.max(1, this.pageNumber - Math.floor(maxPagesToShow / 2));
    let endPage = Math.min(this.totalPages, startPage + maxPagesToShow - 1);

    if (endPage - startPage < maxPagesToShow - 1) {
      startPage = Math.max(1, endPage - maxPagesToShow + 1);
    }

    for (let i = startPage; i <= endPage; i++) {
      pages.push(i);
    }

    return pages;
  }
}
