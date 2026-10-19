import {Component,OnInit,inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpErrorResponse} from '@angular/common/http';
import {ApiService,Member,Listing,Draft} from './api.service';
@Component({selector:'app-root',standalone:true,imports:[CommonModule,FormsModule],templateUrl:'./app.component.html'})
export class AppComponent implements OnInit {
 api=inject(ApiService);user:Member|null=null;items:Listing[]=[];pending:Member[]=[];
 categories=['Tecnología','Ingeniería','Fabricación','Capacitación','Otros'];
 q='';type='';org='';category='';mine=false;loading=true;busy=false;error='';notice='';modal:''|'login'|'register'|'listing'|'admin'='';detail:Listing|null=null;editingId?:string;
 credentials={name:'',email:'',password:'',organization:'UTN FRSN'};
 draft:Draft=this.emptyDraft();
 private requestVersion=0;
 emptyDraft():Draft{return {title:'',description:'',type:'Producto',category:'Tecnología',price:null,contact:this.user?.email??'',imageData:null};}
 async ngOnInit(){try{await this.api.csrf();try{this.user=await this.api.me();}catch(e){if(!(e instanceof HttpErrorResponse&&e.status===401))throw e;}if(this.user)await this.refresh();}catch(e){this.fail(e);}finally{this.loading=false;}}
 fail(e:unknown){this.error=e instanceof HttpErrorResponse?(e.error?.error||({401:'Iniciá sesión nuevamente.',403:'No tenés acceso a esta operación.',429:'Demasiados intentos. Esperá un minuto.'} as Record<number,string>)[e.status]||'No se pudo conectar con el servidor.'):e instanceof Error?e.message:'No se pudo completar la operación.';}
 async refresh(){const version=++this.requestVersion;this.loading=true;this.error='';try{const rows=await this.api.listings({q:this.q,type:this.type,org:this.org,category:this.category,mine:String(this.mine)});if(version===this.requestVersion)this.items=rows;}catch(e){if(version===this.requestVersion)this.fail(e);}finally{if(version===this.requestVersion)this.loading=false;}}
 openAuth(mode:'login'|'register'){this.error='';this.credentials.password='';this.modal=mode;}
 async authenticate(){this.busy=true;this.error='';try{await this.api.csrf();if(this.modal==='register'){await this.api.register(this.credentials);this.notice='Registro recibido. Un administrador debe verificar y aprobar tu pertenencia a la comunidad.';this.modal='login';}else{this.user=await this.api.login(this.credentials.email,this.credentials.password);await this.api.csrf();this.modal='';this.notice='';await this.refresh();}this.credentials.password='';}catch(e){this.fail(e);}finally{this.busy=false;}}
 async logout(){this.busy=true;try{await this.api.logout();this.user=null;this.items=[];this.pending=[];this.detail=null;this.modal='';await this.api.csrf();}catch(e){this.fail(e);}finally{this.busy=false;}}
 showMine(value:boolean){this.mine=value;if(this.user)void this.refresh();else this.openAuth('login');}
 publish(item?:Listing){if(!this.user){this.openAuth('login');return;}this.error='';this.editingId=item?.id;this.draft=item?{title:item.title,description:item.description,type:item.type,category:item.category,price:item.price,contact:item.contact,imageData:null}:this.emptyDraft();this.detail=null;this.modal='listing';}
 async photo(event:Event){const input=event.target as HTMLInputElement;const file=input.files?.[0];if(!file)return;if(file.size>1_000_000||!['image/jpeg','image/png','image/webp'].includes(file.type)){this.error='Elegí JPG, PNG o WebP de hasta 1 MB.';input.value='';return;}try{this.draft.imageData=await new Promise<string>((resolve,reject)=>{const r=new FileReader();r.onload=()=>resolve(String(r.result));r.onerror=()=>reject(new Error('No se pudo leer la imagen.'));r.readAsDataURL(file);});this.error='';}catch(e){this.fail(e);}}
 async save(){this.busy=true;this.error='';try{await this.api.save(this.draft,this.editingId);this.modal='';this.notice='Publicación guardada.';await this.refresh();}catch(e){this.fail(e);}finally{this.busy=false;}}
 async remove(item:Listing){if(!confirm('¿Eliminar esta publicación?'))return;this.busy=true;try{await this.api.delete(item.id);this.detail=null;await this.refresh();}catch(e){this.fail(e);}finally{this.busy=false;}}
 async administration(){this.modal='admin';this.error='';this.busy=true;try{this.pending=await this.api.pending();}catch(e){this.fail(e);}finally{this.busy=false;}}
 async approve(member:Member){if(!confirm('Confirmá que verificaste la pertenencia de '+member.name+' a '+member.organization+'.'))return;this.busy=true;try{await this.api.approve(member.id);this.pending=this.pending.filter(x=>x.id!==member.id);}catch(e){this.fail(e);}finally{this.busy=false;}}
 money(price:number|null){return price===null?'A convenir':new Intl.NumberFormat('es-AR',{style:'currency',currency:'ARS'}).format(price);}
 explore(){document.getElementById('tienda')?.scrollIntoView({behavior:'smooth'});}
 close(){if(!this.busy){this.modal='';this.detail=null;this.error='';}}
}
