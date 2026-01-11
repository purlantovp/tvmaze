import { Component, OnInit } from '@angular/core';
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
    private tvmazeService: TvmazeService
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
    this.tvmazeService.getShowById(id).subscribe({
      next: (show: Show) => {
        this.show = show;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading show:', error);
        this.error = 'Show not found or error loading data.';
        this.loading = false;
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
