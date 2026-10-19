using Conecta.Api.Data;
using Conecta.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Conecta.Api.Services;
public sealed class MemberService(ConectaDb db, IPasswordHasher<Member> hasher) {
 public static MemberView View(Member m) => new(m.Id,m.Name,m.Email,m.Organization,m.IsAdmin,m.Approved);
 public async Task Register(RegisterRequest r) {
  var name = Rules.Text(r.Name,2,120,"Nombre"); var email = Rules.Email(r.Email); Rules.Password(r.Password);
  if (!Rules.Organizations.Contains(r.Organization)) throw new BusinessException("Comunidad inválida.");
  if (await db.Members.AnyAsync(x => x.Email == email)) throw new BusinessException("Ese email ya está registrado.",409);
  var member = new Member { Name=name,Email=email,Organization=r.Organization };
  member.PasswordHash=hasher.HashPassword(member,r.Password); db.Members.Add(member);
  try { await db.SaveChangesAsync(); } catch (DbUpdateException) { throw new BusinessException("No se pudo registrar. Revisá si el email ya existe.",409); }
 }
 public async Task<Member> Login(LoginRequest r) {
  var email=Rules.Email(r.Email);
  if (string.IsNullOrEmpty(r.Password) || r.Password.Length > 128) throw new BusinessException("Email o contraseña incorrectos.",401);
  var m=await db.Members.SingleOrDefaultAsync(x=>x.Email==email);
  if (m is null || m.LockedUntil > DateTimeOffset.UtcNow) throw new BusinessException("Email o contraseña incorrectos, o cuenta temporalmente bloqueada.",401);
  var result=hasher.VerifyHashedPassword(m,m.PasswordHash,r.Password);
  if (result == PasswordVerificationResult.Failed) {
   m.FailedLogins++;
   if (m.FailedLogins >= 5) { m.LockedUntil=DateTimeOffset.UtcNow.AddMinutes(15); m.FailedLogins=0; }
   await db.SaveChangesAsync(); throw new BusinessException("Email o contraseña incorrectos.",401);
  }
  if (!m.Approved) throw new BusinessException("Tu cuenta está pendiente de aprobación institucional.",403);
  m.FailedLogins=0; m.LockedUntil=null;
  if (result==PasswordVerificationResult.SuccessRehashNeeded) m.PasswordHash=hasher.HashPassword(m,r.Password);
  await db.SaveChangesAsync(); return m;
 }
 public async Task SeedAdmin(string email, string password, string organization) {
  email=Rules.Email(email); Rules.Password(password);
  if (!Rules.Organizations.Contains(organization)) throw new BusinessException("Bootstrap:Organization inválida.");
  if (await db.Members.AnyAsync(x=>x.IsAdmin)) throw new BusinessException("Ya existe un administrador. No se modificó ninguna cuenta.");
  if (await db.Members.AnyAsync(x=>x.Email==email)) throw new BusinessException("El email ya existe. Usá otro para crear el administrador.");
  var m=new Member {Name="Administración",Email=email,Organization=organization,Approved=true,IsAdmin=true};
  m.PasswordHash=hasher.HashPassword(m,password); db.Members.Add(m); await db.SaveChangesAsync();
 }
}
