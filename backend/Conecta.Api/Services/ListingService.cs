using Conecta.Api.Data;
using Conecta.Api.Domain;
using Microsoft.EntityFrameworkCore;
namespace Conecta.Api.Services;
public sealed class ListingService(ConectaDb db) {
 public async Task<IReadOnlyList<ListingView>> Search(string? q, string? type, string? org, string? category, Guid? owner) {
  var query=db.Listings.AsNoTracking().Where(x=>x.Owner.Approved);
  if (!string.IsNullOrWhiteSpace(q)) { q=Rules.Text(q,1,100,"Búsqueda"); query=query.Where(x=>x.Title.Contains(q)||x.Description.Contains(q)); }
  if (!string.IsNullOrEmpty(type)) query=query.Where(x=>x.Type==type);
  if (!string.IsNullOrEmpty(org)) query=query.Where(x=>x.Owner.Organization==org);
  if (!string.IsNullOrEmpty(category)) query=query.Where(x=>x.Category==category);
  if (owner.HasValue) query=query.Where(x=>x.OwnerId==owner);
  return await query.OrderByDescending(x=>x.CreatedAt).Select(x=>new ListingView(x.Id,x.OwnerId,x.Owner.Name,x.Owner.Organization,x.Title,x.Description,x.Type,x.Category,x.Price,x.Contact,x.Image==null?null:"/api/listings/"+x.Id+"/image",x.CreatedAt)).ToListAsync();
 }
 public async Task<Guid> Save(Guid owner, Guid? id, ListingRequest r) {
  Rules.Listing(r); var photo=Rules.Photo(r.ImageData);
  Listing listing;
  if (id.HasValue) { listing=await db.Listings.SingleOrDefaultAsync(x=>x.Id==id.Value) ?? throw new BusinessException("Publicación inexistente.",404); Owns(listing,owner); }
  else { listing=new Listing {OwnerId=owner}; db.Listings.Add(listing); }
  listing.Title=r.Title.Trim(); listing.Description=r.Description.Trim(); listing.Type=r.Type; listing.Category=r.Category; listing.Price=r.Price; listing.Contact=Rules.Email(r.Contact);
  // null preserves the existing image when editing; an empty string removes it.
  if (!id.HasValue || r.ImageData is not null) { listing.Image=photo.Bytes; listing.ImageMime=photo.Mime; }
  listing.UpdatedAt=DateTimeOffset.UtcNow;
  await db.SaveChangesAsync(); return listing.Id;
 }
 public async Task Delete(Guid owner,Guid id) {
  var listing=await db.Listings.FindAsync(id) ?? throw new BusinessException("Publicación inexistente.",404);
  Owns(listing,owner); db.Listings.Remove(listing); await db.SaveChangesAsync();
 }
 private static void Owns(Listing item,Guid owner) { if (item.OwnerId!=owner) throw new BusinessException("Solo podés modificar tus propias publicaciones.",403); }
}
