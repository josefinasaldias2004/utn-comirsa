using Conecta.Api.Data;
using Conecta.Api.Services;
using Conecta.Api.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var email = Rules.Email(Environment.GetEnvironmentVariable("Bootstrap__Email"));
var password = Environment.GetEnvironmentVariable("Bootstrap__Password");
Rules.Password(password);

var databasePath = Environment.GetEnvironmentVariable("Conecta__LocalDatabase");
if (string.IsNullOrWhiteSpace(databasePath) || !File.Exists(databasePath))
    throw new InvalidOperationException("No se encontró la base SQLite local; no se modificó ninguna cuenta.");

var options = new DbContextOptionsBuilder<ConectaDb>()
    .UseSqlite($"Data Source={databasePath};Mode=ReadWrite")
    .Options;

await using var db = new ConectaDb(options);
var member = await db.Members.SingleOrDefaultAsync(x => x.Email == email)
    ?? throw new InvalidOperationException("No existe una cuenta local con ese email.");

if (member.IsAdmin)
    throw new InvalidOperationException("La cuenta ya es administradora; no se modificó.");
if (!Rules.Organizations.Contains(member.Organization))
    throw new InvalidOperationException("La comunidad de la cuenta no es válida; no se modificó.");

member.PasswordHash = new PasswordHasher<Member>().HashPassword(member, password!);
member.Approved = true;
member.IsAdmin = true;
member.FailedLogins = 0;
member.LockedUntil = null;
await db.SaveChangesAsync();

Console.WriteLine($"Cuenta local activada como administradora: {email}");
