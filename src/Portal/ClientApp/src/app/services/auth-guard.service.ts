import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';
import { PortalClaimRequired } from '../services/auth.service';
import { GPTWEmployeeOnly } from '../services/auth.service';
import { AuthenticationOnly } from '../services/auth.service';
import { OpenAccess } from '../services/auth.service';

@Injectable()
export class AuthGuardService implements CanActivate {

  constructor(private authService: AuthService, private router: Router) { }

    canActivate(route: ActivatedRouteSnapshot): boolean {

      console.log("canActivate start")

      if (route != null && route.url.length > 0) {
        let url: string = "";
        for (let part of route.url) {
          url += "/" + part.path;
        }
        console.log("canActivate-url:" + url)
        if (url != "/error/downformaintenance") {
          if (this.authService.isAppMaintenanceMode == 'yes' && !this.authService.isExemptFromMaintanencePage())
            this.router.navigate(["/error/downformaintenance"]); //then redirect to the down for maintenance page
        }
      }

      // If I have no route access data you aren't getting in
      if (route.data == undefined)
      {
          this.router.navigate(["/error/noaccess"]);
          return false;
      }

      console.log("canActivate 2")

      // IF the component is open then everyone gets in.
      if (route.data.access == OpenAccess)
      {
            return true;
      }

      console.log("canActivate 3")

      if (this.authService.isLoggedIn())
      {
        // if this is an employee and they don't have env claim return false immediately
        if (this.authService.isEmployee() && !this.authService.doesEmployeeHaveExpectedEnvClaim()) {
          this.router.navigate(["/error/noaccess"]);
          return false;
        }

        console.log("canActivate 4")

        // if Authentication only then we're done
        if (route.data.access == AuthenticationOnly) {
              return true;
        }

        console.log("canActivate 5")

        // If this component has been decorated with GPTWEmployeeOnly Access then check and route to no access if they aren't an employee
        if (route.data.access == GPTWEmployeeOnly) {
            if (!this.authService.isEmployee()) {
                this.router.navigate(["/error/noaccess"]);
                return false;
          }

          return true;
        }

        console.log("canActivate 6")

        // If we are at this point in the code they must have the PortalClaimRequired or some configuration has been
        // accidently not entered so we are going to bail which will force the developer to enter the correct configuration 
        if (route.data.access != PortalClaimRequired) {
            this.router.navigate(["/error/noaccess"]);
            return false;
        }

        console.log("canActivate 7")

        if (!this.authService.isEmployeeWithPortalClaim() && !this.authService.isEndUserWithPortalClaim()) {
            this.router.navigate(["/error/noportalclaim"]);
            return false;
        }

        console.log("canActivate 8")

        return true;
      }
      else
      {
          console.log("canActivate 9")

          localStorage.returnUrl = "";

          if (route != null && route.url.length > 0) {
            let url: string = "";
            for (let part of route.url) {
                url += "/" + part.path;
            }

            console.log("canActivate 10")
            // We need this in order to support querystring parameter passing which ZenDesk does to tell us where to return to
            if (url.toLowerCase() == "/helpdesksso" || url.toLowerCase() == "/helpdesksso2") {
                if (route.queryParams && route.queryParams.return_to)
                    localStorage.zendeskReturnTo = route.queryParams.return_to;
            }
            console.log("storing url:" + url);
            localStorage.returnUrl = url;
      }
    }

    console.log("canActivate 11")

    this.authService.startAuthentication();
    return false;
  }
}
