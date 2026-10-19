import {Injectable,inject} from '@angular/core';
import {HttpClient,HttpParams} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
export interface Member {id:string;name:string;email:string;organization:string;isAdmin:boolean;approved:boolean}
export interface Listing {id:string;ownerId:string;name:string;organization:string;title:string;description:string;type:string;category:string;price:number|null;contact:string;imageUrl:string|null;createdAt:string}
export interface Draft {title:string;description:string;type:string;category:string;price:number|null;contact:string;imageData:string|null}
@Injectable({providedIn:'root'})
export class ApiService {
 private http=inject(HttpClient);
 csrf(){return firstValueFrom(this.http.get('/api/auth/csrf'));}
 me(){return firstValueFrom(this.http.get<Member>('/api/auth/me'));}
 login(email:string,password:string){return firstValueFrom(this.http.post<Member>('/api/auth/login',{email,password}));}
 register(data:{name:string;email:string;password:string;organization:string}){return firstValueFrom(this.http.post('/api/auth/register',data));}
 logout(){return firstValueFrom(this.http.post('/api/auth/logout',{}));}
 listings(filters:Record<string,string>){return firstValueFrom(this.http.get<Listing[]>('/api/listings',{params:new HttpParams({fromObject:filters})}));}
 save(draft:Draft,id?:string){return firstValueFrom(id?this.http.put('/api/listings/'+id,draft):this.http.post('/api/listings',draft));}
 delete(id:string){return firstValueFrom(this.http.delete('/api/listings/'+id));}
 pending(){return firstValueFrom(this.http.get<Member[]>('/api/members/pending'));}
 approve(id:string){return firstValueFrom(this.http.post('/api/members/'+id+'/approve',{}));}
}
