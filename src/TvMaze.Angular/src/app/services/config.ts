import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';

export interface AppConfig {
  apiUrl: string;
}

@Injectable({
  providedIn: 'root',
})
export class ConfigService {
  private config?: AppConfig;

  constructor(private http: HttpClient) {}

  async loadConfig(): Promise<void> {
    try {
      this.config = await firstValueFrom(
        this.http.get<AppConfig>('/assets/config.json')
      );
    } catch (error) {
      console.error('Failed to load configuration:', error);
      // Fallback to default configuration
      this.config = {
        apiUrl: 'http://localhost:5000/api',
      };
    }
  }

  get apiUrl(): string {
    return this.config?.apiUrl || 'http://localhost:5000/api';
  }
}
