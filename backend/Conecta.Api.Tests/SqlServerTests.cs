using Conecta.Api.Data;
using Conecta.Api.Domain;
using Conecta.Api.Services;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Conecta.Api.Tests;
public sealed class SqlServerFactAttribute : FactAttribute {
 public SqlServerFactAttribute() {
  if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SQL_TEST_CONNECTION")))
   Skip="SQL Server no está configurado. Definí SQL_TEST_CONNECTION para esta prueba de integración.";
 }
}
public sealed class SqlServerTests {
 [SqlServerFact]
 public async Task Sql_server_persists_and_filters_listings_across_contexts() {
  var connection=new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SQL_TEST_CONNECTION")) {
   InitialCatalog="Conecta_Test_"+Guid.NewGuid().ToString("N")
  };
  var options=new DbContextOptionsBuilder<ConectaDb>().UseSqlServer(connection.ConnectionString).Options;
  await using var db=new ConectaDb(options);
  try {
   await db.Database.EnsureCreatedAsync();
   var owner=new Member {Name="Empresa de prueba",Email="sql-test@example.com",Organization="COMIRSA",Approved=true};
   db.Members.Add(owner);await db.SaveChangesAsync();
   var id=await new ListingService(db).Save(owner.Id,null,new("Diseño industrial","Servicio de ingeniería y diseño industrial.","Servicio","Ingeniería",1234.56m,"sql-test@example.com",null));
   await using var secondContext=new ConectaDb(options);
   var service=new ListingService(secondContext);
   var found=Assert.Single(await service.Search("Diseño","Servicio","COMIRSA","Ingeniería",null));
   Assert.Equal(id,found.Id);Assert.Equal(1234.56m,found.Price);
   Assert.Empty(await service.Search(null,null,"UTN FRSN",null,null));
   await service.Delete(owner.Id,id);
   Assert.Empty(await service.Search(null,null,null,null,null));
  } finally {
   // The test creates a fresh randomly named database and only deletes that database.
   if(connection.InitialCatalog.StartsWith("Conecta_Test_",StringComparison.Ordinal))
    await db.Database.EnsureDeletedAsync();
  }
 }
}
