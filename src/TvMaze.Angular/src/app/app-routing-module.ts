import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ShowList } from './components/show-list/show-list';
import { ShowDetails } from './components/show-details/show-details';
import { Scraper } from './components/scraper/scraper';

export const routes: Routes = [
  { path: '', component: ShowList },
  { path: 'show/:id', component: ShowDetails },
  { path: 'scraper', component: Scraper },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
