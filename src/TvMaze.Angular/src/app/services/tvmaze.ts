import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, Show } from '../models/show.model';
import { ConfigService } from './config';

@Injectable({
  providedIn: 'root',
})
export class TvmazeService {
  private get apiUrl(): string {
    return this.configService.apiUrl;
  }

  constructor(
    private http: HttpClient,
    private configService: ConfigService
  ) {}

  getShows(
    pageNumber: number = 1,
    pageSize: number = 10,
    orderBy?: string,
    searchTerm?: string
  ): Observable<PagedResult<Show>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (orderBy) {
      params = params.set('orderBy', orderBy);
    }

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PagedResult<Show>>(`${this.apiUrl}/shows`, { params });
  }

  getShowById(id: number): Observable<Show> {
    return this.http.get<Show>(`${this.apiUrl}/shows/${id}`);
  }

  getShowCount(): Observable<number> {
    return this.http.get<number>(`${this.apiUrl}/shows/count`);
  }
}
