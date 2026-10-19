import {bootstrapApplication} from '@angular/platform-browser';
import {provideHttpClient,withXsrfConfiguration} from '@angular/common/http';
import {provideZoneChangeDetection} from '@angular/core';
import {AppComponent} from './app/app.component';
bootstrapApplication(AppComponent,{providers:[provideZoneChangeDetection(),provideHttpClient(withXsrfConfiguration({cookieName:'XSRF-TOKEN',headerName:'X-XSRF-TOKEN'}))]}).catch(console.error);
