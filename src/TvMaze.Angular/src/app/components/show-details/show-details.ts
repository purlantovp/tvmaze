import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TvmazeService } from '../../services/tvmaze';
import { Show } from '../../models/show.model';

@Component({
  selector: 'app-show-details',
  standalone: false,
  templateUrl: './show-details.html',
  styleUrl: './show-details.css',
})
export class ShowDetails implements OnInit {
  show: Show | null = null;
  loading: boolean = false;
  error: string = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private tvmazeService: TvmazeService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadShow(+id);
    }
  }

  loadShow(id: number): void {
    this.loading = true;
    this.error = '';
    console.log('Loading show with id:', id);
    this.tvmazeService.getShowById(id).subscribe({
      next: (show: Show) => {
        console.log('Received show:', show);
        this.show = show;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        console.error('Error loading show:', error);
        this.error = 'Show not found or error loading data.';
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/']);
  }

  formatBirthday(birthday: string | null): string {
    if (!birthday) return 'N/A';
    const date = new Date(birthday);
    return date.toLocaleDateString();
  }
}
