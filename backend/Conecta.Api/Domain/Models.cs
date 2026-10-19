namespace Conecta.Api.Domain;
public sealed class Member {
 public Guid Id { get; set; } = Guid.NewGuid();
 public string Name { get; set; } = "";
 public string Email { get; set; } = "";
 public string PasswordHash { get; set; } = "";
 public string Organization { get; set; } = "";
 public bool Approved { get; set; }
 public bool IsAdmin { get; set; }
 public int FailedLogins { get; set; }
 public DateTimeOffset? LockedUntil { get; set; }
 public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed class Listing {
 public Guid Id { get; set; } = Guid.NewGuid();
 public Guid OwnerId { get; set; }
 public Member Owner { get; set; } = null!;
 public string Title { get; set; } = "";
 public string Description { get; set; } = "";
 public string Type { get; set; } = "";
 public string Category { get; set; } = "";
 public decimal? Price { get; set; }
 public string Contact { get; set; } = "";
 public byte[]? Image { get; set; }
 public string? ImageMime { get; set; }
 public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
 public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public sealed record RegisterRequest(string Name, string Email, string Password, string Organization);
public sealed record LoginRequest(string Email, string Password);
public sealed record ListingRequest(string Title, string Description, string Type, string Category, decimal? Price, string Contact, string? ImageData);
public sealed record MemberView(Guid Id, string Name, string Email, string Organization, bool IsAdmin, bool Approved);
public sealed record ListingView(Guid Id, Guid OwnerId, string Name, string Organization, string Title, string Description, string Type, string Category, decimal? Price, string Contact, string? ImageUrl, DateTimeOffset CreatedAt);
public sealed class BusinessException(string message, int status = 400) : Exception(message) {
 public int Status { get; } = status;
}
