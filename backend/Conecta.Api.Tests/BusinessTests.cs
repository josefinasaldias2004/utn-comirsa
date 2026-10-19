using Conecta.Api.Data;
using Conecta.Api.Domain;
using Conecta.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace Conecta.Api.Tests;

public class BusinessTests : IDisposable {
 private readonly SqliteConnection connection = new("Data Source=:memory:");
 private readonly ConectaDb db;
 public BusinessTests() {
  connection.Open();
  db=new ConectaDb(new DbContextOptionsBuilder<ConectaDb>().UseSqlite(connection).Options);
  db.Database.EnsureCreated();
 }
 private async Task<Member> Member(string email) {
  var member=new Member {Name="Miembro",Email=email,Organization="UTN FRSN",Approved=true};
  member.PasswordHash=new PasswordHasher<Member>().HashPassword(member,"ClavePrueba_12345");
  db.Members.Add(member);await db.SaveChangesAsync();return member;
 }
 private static ListingRequest Draft(decimal? price=1500.25m)=>new("Diseño de piezas","Servicio de diseño y prototipado industrial.","Servicio","Ingeniería",price,"ventas@example.com",null);
 [Fact] public async Task Registration_requires_approval_and_hashes_password() {
  var svc=new MemberService(db,new PasswordHasher<Member>());
  await svc.Register(new("Nombre Apellido","MEMBER@example.com","ClavePrueba_12345","COMIRSA"));
  var m=await db.Members.SingleAsync();
  Assert.False(m.Approved);Assert.False(m.IsAdmin);Assert.Equal("member@example.com",m.Email);
  Assert.NotEqual("ClavePrueba_12345",m.PasswordHash);
  var e=await Assert.ThrowsAsync<BusinessException>(()=>svc.Login(new(m.Email,"ClavePrueba_12345")));
  Assert.Equal(403,e.Status);
 }
 [Fact] public async Task Another_member_cannot_edit_or_delete_a_listing() {
  var owner=await Member("owner@example.com");var stranger=await Member("stranger@example.com");
  var svc=new ListingService(db);var id=await svc.Save(owner.Id,null,Draft());
  Assert.Equal(403,(await Assert.ThrowsAsync<BusinessException>(()=>svc.Save(stranger.Id,id,Draft()))).Status);
  Assert.Equal(403,(await Assert.ThrowsAsync<BusinessException>(()=>svc.Delete(stranger.Id,id))).Status);
  db.ChangeTracker.Clear();Assert.Equal(1500.25m,(await db.Listings.SingleAsync()).Price);
  await svc.Delete(owner.Id,id);Assert.Empty(await db.Listings.ToListAsync());
 }
 [Fact] public async Task Invalid_price_does_not_write_data() {
  var m=await Member("owner@example.com");var svc=new ListingService(db);
  await Assert.ThrowsAsync<BusinessException>(()=>svc.Save(m.Id,null,Draft(-1)));
  await Assert.ThrowsAsync<BusinessException>(()=>svc.Save(m.Id,null,Draft(1.123m)));
  Assert.Empty(await db.Listings.ToListAsync());
 }
 [Fact] public async Task Repeated_failed_logins_lock_account() {
  var m=await Member("owner@example.com");var svc=new MemberService(db,new PasswordHasher<Member>());
  for(var i=0;i<5;i++)await Assert.ThrowsAsync<BusinessException>(()=>svc.Login(new(m.Email,"incorrecta")));
  Assert.True(m.LockedUntil>DateTimeOffset.UtcNow);
  await Assert.ThrowsAsync<BusinessException>(()=>svc.Login(new(m.Email,"ClavePrueba_12345")));
 }
 [Fact] public void Executable_or_svg_images_are_rejected() {
  Assert.Throws<BusinessException>(()=>Rules.Photo("data:image/svg+xml;base64,PHN2Zz4="));
  Assert.Throws<BusinessException>(()=>Rules.Photo("data:image/png;base64,"+Convert.ToBase64String(new byte[32])));
 }
 public void Dispose(){db.Dispose();connection.Dispose();}
}
