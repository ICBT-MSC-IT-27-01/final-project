import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  template: `
    <h1>AI-Driven Weather Forecasting and Crop Recommendation System</h1>

    <router-outlet />
  `,
  styles: [],
})
export class App {
}
