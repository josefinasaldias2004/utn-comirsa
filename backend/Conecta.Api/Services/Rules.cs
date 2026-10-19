using System.Net.Mail;
using Conecta.Api.Domain;
namespace Conecta.Api.Services;
public static class Rules {
 public static readonly string[] Organizations = ["UTN FRSN", "COMIRSA"];
 public static readonly string[] Categories = ["Tecnología", "Ingeniería", "Fabricación", "Capacitación", "Otros"];
 public static string Text(string? value, int min, int max, string label) {
  var clean = value?.Trim() ?? "";
  if (clean.Length < min || clean.Length > max) throw new BusinessException($"{label}: entre {min} y {max} caracteres.");
  return clean;
 }
 public static string Email(string? value) {
  var email = Text(value, 3, 254, "Email").ToLowerInvariant();
  if (!MailAddress.TryCreate(email, out var parsed) || parsed.Address != email) throw new BusinessException("Email inválido.");
  return email;
 }
 public static void Password(string? value) {
  if (value is null || value.Length < 12 || value.Length > 128) throw new BusinessException("La contraseña debe tener entre 12 y 128 caracteres.");
 }
 public static (byte[]? Bytes, string? Mime) Photo(string? data) {
  if (string.IsNullOrEmpty(data)) return (null,null);
  if (data.Length > 1_400_000) throw new BusinessException("La imagen supera 1 MB.");
  var comma = data.IndexOf(',');
  if (comma < 0) throw new BusinessException("Imagen inválida.");
  var mime = data[..comma] switch { "data:image/png;base64" => "image/png", "data:image/jpeg;base64" => "image/jpeg", "data:image/webp;base64" => "image/webp", _ => throw new BusinessException("Usá PNG, JPG o WebP.") };
  byte[] bytes;
  try { bytes = Convert.FromBase64String(data[(comma+1)..]); } catch (FormatException) { throw new BusinessException("Imagen inválida."); }
  if (bytes.Length is < 12 or > 1_000_000) throw new BusinessException("Imagen inválida o mayor a 1 MB.");
  var valid = mime switch {
   "image/png" => bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),
   "image/jpeg" => bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
   _ => System.Text.Encoding.ASCII.GetString(bytes,0,4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes,8,4) == "WEBP"
  };
  if (!valid) throw new BusinessException("El archivo no corresponde al formato de imagen.");
  return (bytes,mime);
 }
 public static void Listing(ListingRequest request) {
  Text(request.Title,5,100,"Título"); Text(request.Description,20,3000,"Descripción");
  if (request.Type is not ("Producto" or "Servicio")) throw new BusinessException("Tipo inválido.");
  if (!Categories.Contains(request.Category)) throw new BusinessException("Categoría inválida.");
  if (request.Price is < 0 or > 1_000_000_000 || request.Price is decimal p && decimal.Round(p,2) != p) throw new BusinessException("Precio inválido: usá hasta dos decimales.");
  Email(request.Contact);
 }
}
