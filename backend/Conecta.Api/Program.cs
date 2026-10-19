using System.Security.Claims;
using System.Threading.RateLimiting;
using Conecta.Api.Data;
using Conecta.Api.Domain;
using Conecta.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder=WebApplication.CreateBuilder(args);
// Console logging works in containers and does not require a Windows Event Log source.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddDbContext<ConectaDb>(o=>o.UseSqlServer(builder.Configuration.GetConnectionString("Default")
 ?? throw new InvalidOperationException("Configurá ConnectionStrings__Default.")));
builder.Services.AddScoped<IPasswordHasher<Member>,PasswordHasher<Member>>();
builder.Services.AddScoped<MemberService>(); builder.Services.AddScoped<ListingService>();
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:Path"] ?? Path.Combine(builder.Environment.ContentRootPath,"keys")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
 o.Cookie.Name="conecta.session"; o.Cookie.HttpOnly=true; o.Cookie.SameSite=SameSiteMode.Strict;
 o.Cookie.SecurePolicy=builder.Environment.IsDevelopment()?CookieSecurePolicy.SameAsRequest:CookieSecurePolicy.Always;
 o.ExpireTimeSpan=TimeSpan.FromHours(8); o.SlidingExpiration=false;
 o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};
 o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
 o.Events.OnValidatePrincipal=async c=>{
  var id=c.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
  var db=c.HttpContext.RequestServices.GetRequiredService<ConectaDb>();
  if (!Guid.TryParse(id,out var uid) || !await db.Members.AnyAsync(x=>x.Id==uid&&x.Approved)) { c.RejectPrincipal(); await c.HttpContext.SignOutAsync(); }
 };
});
builder.Services.AddAuthorization(o=>o.AddPolicy("Admin",p=>p.RequireRole("Admin")));
builder.Services.AddAntiforgery(o=>{o.HeaderName="X-XSRF-TOKEN";o.Cookie.SameSite=SameSiteMode.Strict;});
builder.Services.AddRateLimiter(o=>{
 o.RejectionStatusCode=429;
 o.AddPolicy("auth",ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions { PermitLimit=15,Window=TimeSpan.FromMinutes(1),QueueLimit=0 }));
});
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=1_500_000);
var app=builder.Build();
if (args.Contains("--init-db") || args.Contains("--seed-admin")) {
 await using var scope=app.Services.CreateAsyncScope();
 if(args.Contains("--init-db")) await scope.ServiceProvider.GetRequiredService<ConectaDb>().Database.EnsureCreatedAsync();
 if(args.Contains("--seed-admin")) await scope.ServiceProvider.GetRequiredService<MemberService>().SeedAdmin(
  builder.Configuration["Bootstrap:Email"]??"",builder.Configuration["Bootstrap:Password"]??"",builder.Configuration["Bootstrap:Organization"]??"UTN FRSN");
 return;
}
app.Use(async(ctx,next)=>{
 try { await next(); }
 catch (BusinessException e) { ctx.Response.StatusCode=e.Status; await ctx.Response.WriteAsJsonAsync(new {error=e.Message}); }
 catch (AntiforgeryValidationException) { ctx.Response.StatusCode=400;await ctx.Response.WriteAsJsonAsync(new {error="La sesión del formulario venció. Recargá la página."}); }
 catch(Exception e) { app.Logger.LogError(e,"Request failed");ctx.Response.StatusCode=500;await ctx.Response.WriteAsJsonAsync(new {error="No se pudo completar la operación."}); }
});
app.UseAuthentication();app.UseAuthorization();app.UseRateLimiter();
app.Use(async(ctx,next)=>{
 ctx.Response.Headers["X-Content-Type-Options"]="nosniff";
 if (ctx.Request.Path.StartsWithSegments("/api")) {
  ctx.Response.Headers.CacheControl="no-store";
  if (ctx.Request.Method is "POST" or "PUT" or "PATCH" or "DELETE") await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx);
 }
 await next();
});
Guid UserId(HttpContext c)=>Guid.Parse(c.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
app.MapGet("/api/health",()=>Results.Ok(new {status="ok"}));
app.MapGet("/api/auth/csrf",(HttpContext c,IAntiforgery anti)=>{
 var tokens=anti.GetAndStoreTokens(c);
 c.Response.Cookies.Append("XSRF-TOKEN",tokens.RequestToken!,new CookieOptions { HttpOnly=false,SameSite=SameSiteMode.Strict,Secure=!app.Environment.IsDevelopment(),Path="/" });
 return Results.NoContent();
});
app.MapPost("/api/auth/register",async(RegisterRequest r,MemberService svc)=>{await svc.Register(r);return Results.Ok(new {message="Solicitud recibida. Esperá la aprobación institucional."});}).RequireRateLimiting("auth");
app.MapPost("/api/auth/login",async(LoginRequest r,MemberService svc,HttpContext c)=>{
 var m=await svc.Login(r);
 var claims=new List<Claim>{new(ClaimTypes.NameIdentifier,m.Id.ToString()),new(ClaimTypes.Name,m.Name)};
 if(m.IsAdmin) claims.Add(new(ClaimTypes.Role,"Admin"));
 await c.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
 return Results.Ok(MemberService.View(m));
}).RequireRateLimiting("auth");
app.MapPost("/api/auth/logout",async(HttpContext c)=>{await c.SignOutAsync();return Results.NoContent();}).RequireAuthorization();
app.MapGet("/api/auth/me",async(HttpContext c,ConectaDb db)=>{ var id=UserId(c); return Results.Ok(MemberService.View(await db.Members.SingleAsync(x=>x.Id==id))); }).RequireAuthorization();
app.MapGet("/api/members/pending",async(ConectaDb db)=>await db.Members.AsNoTracking().Where(x=>!x.Approved).OrderBy(x=>x.CreatedAt).Select(x=>new MemberView(x.Id,x.Name,x.Email,x.Organization,x.IsAdmin,x.Approved)).ToListAsync()).RequireAuthorization("Admin");
app.MapPost("/api/members/{id:guid}/approve",async Task<IResult>(Guid id,ConectaDb db)=>{
 var m=await db.Members.FindAsync(id);if(m is null)return Results.NotFound();
 m.Approved=true;await db.SaveChangesAsync();return Results.NoContent();
}).RequireAuthorization("Admin");
app.MapGet("/api/listings",async(string? q,string? type,string? org,string? category,bool? mine,HttpContext c,ListingService svc)=>await svc.Search(q,type,org,category,mine==true?UserId(c):null)).RequireAuthorization();
app.MapPost("/api/listings",async(ListingRequest r,ListingService svc,HttpContext c)=>Results.Created("/api/listings",new {id=await svc.Save(UserId(c),null,r)})).RequireAuthorization();
app.MapPut("/api/listings/{id:guid}",async(Guid id,ListingRequest r,ListingService svc,HttpContext c)=>{await svc.Save(UserId(c),id,r);return Results.NoContent();}).RequireAuthorization();
app.MapDelete("/api/listings/{id:guid}",async(Guid id,ListingService svc,HttpContext c)=>{await svc.Delete(UserId(c),id);return Results.NoContent();}).RequireAuthorization();
app.MapGet("/api/listings/{id:guid}/image",async Task<IResult>(Guid id,ConectaDb db)=>{
 var item=await db.Listings.AsNoTracking().Where(x=>x.Id==id&&x.Owner.Approved).Select(x=>new {x.Image,x.ImageMime}).SingleOrDefaultAsync();
 return item?.Image is null ? Results.NotFound() : Results.File(item.Image,item.ImageMime??"application/octet-stream");
}).RequireAuthorization();
app.Run();
public partial class Program { }
