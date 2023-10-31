import { Component, OnInit } from '@angular/core';
import { AuthService } from '../services/auth.service'
import { Router } from '@angular/router';
import { AppInsights } from 'applicationinsights-js';

@Component({
  selector: 'app-auth-callback',
  templateUrl: './auth-callback.component.html',
  styleUrls: ['./auth-callback.component.scss']
})
export class AuthCallbackComponent implements OnInit {
  constructor(public authService: AuthService, private router: Router) {
  }

  ngOnInit() {
    this.authService.completeAuthentication();
    if (this.authService.user === null || this.authService.user === undefined) {
      //refresh the auth token and retry
      console.log("lost the cookie. Retrying")

      console.log(this.authService.user);
    }
  }

}
