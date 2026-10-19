using System.Net;
using System.Net.Http.Json;
using Conecta.Api.Data;
using Conecta.Api.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Xunit;
namespace Conecta.Api.Tests;
public sealed class ApiFactory : WebApplicationFactory<Program> {
 private readonly SqliteConnection connection=new("Data Source=:memory:");
 public ApiFactory(){connection.Open();}
 protected override void ConfigureWebHost(IWebHostBuilder builder) {
  builder.UseEnvironment("Development");
  builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
  builder.UseSetting("ConnectionStrings:Default","not-used");
  builder.UseSetting("DataProtection:Path",Path.Combine(Path.GetTempPath(),"conecta-tests-keys"));
  builder.ConfigureServices(s=>{
   s.RemoveAll<DbContextOptions<ConectaDb>>();
   s.RemoveAll<IDbContextOptionsConfiguration<ConectaDb>>();
   s.AddDbContext<ConectaDb>(o=>o.UseSqlite(connection));
  });
 }
 public async Task Initialize(){using var scope=Services.CreateScope();await scope.ServiceProvider.GetRequiredService<ConectaDb>().Database.EnsureCreatedAsync();}
 protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)connection.Dispose();}
}
public class AuthTests {
 [Fact] public async Task Protected_routes_and_csrf_are_enforced() {
  using var factory=new ApiFactory();await factory.Initialize();
  using var client=factory.CreateClient(new WebApplicationFactoryClientOptions{AllowAutoRedirect=false});
  Assert.Equal(HttpStatusCode.Unauthorized,(await client.GetAsync("/api/listings")).StatusCode);
  var registration=new RegisterRequest("Miembro","test@example.com","ClavePrueba_12345","UTN FRSN");
  Assert.Equal(HttpStatusCode.BadRequest,(await client.PostAsJsonAsync("/api/auth/register",registration)).StatusCode);
  var csrf=await client.GetAsync("/api/auth/csrf");
  var cookie=csrf.Headers.GetValues("Set-Cookie").Single(x=>x.StartsWith("XSRF-TOKEN="));
  var token=Uri.UnescapeDataString(cookie.Split(';')[0]["XSRF-TOKEN=".Length..]);
  client.DefaultRequestHeaders.Add("X-XSRF-TOKEN",token);
  Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/register",registration)).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.PostAsJsonAsync("/api/auth/login",new LoginRequest(registration.Email,registration.Password))).StatusCode);
  using(var scope=factory.Services.CreateScope()) {
   var db=scope.ServiceProvider.GetRequiredService<ConectaDb>();
   (await db.Members.SingleAsync()).Approved=true;await db.SaveChangesAsync();
  }
  Assert.Equal(HttpStatusCode.OK,(await client.PostAsJsonAsync("/api/auth/login",new LoginRequest(registration.Email,registration.Password))).StatusCode);
  Assert.Equal(HttpStatusCode.OK,(await client.GetAsync("/api/auth/me")).StatusCode);
  Assert.Equal(HttpStatusCode.Forbidden,(await client.GetAsync("/api/members/pending")).StatusCode);
 }
}
