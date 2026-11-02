using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.Entities;

/// <summary>
/// Refresh token of a login (catalog database). Only the SHA-256 hash is stored, so a leaked database
/// cannot be replayed; the raw token is returned to the client once. Tokens are single use: redeeming one
/// revokes it with reason <see cref="RefreshTokenRevokeReasons.Rotated"/>.
/// </summary>
[Index(nameof(TokenHash), IsUnique = true)]
public class RefreshToken
{
    public int Id { get; set; }

    /// <summary>Hex SHA-256 of the raw token.</summary>
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime Expires { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Revoked { get; set; }

    /// <summary>Why the token was revoked, see <see cref="RefreshTokenRevokeReasons"/>.</summary>
    [MaxLength(30)]
    public string? RevokedReason { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public bool IsActive => Revoked == null && DateTime.UtcNow <= Expires;
}

public static class RefreshTokenRevokeReasons
{
    /// <summary>Exchanged for a new token pair.</summary>
    public const string Rotated = "rotated";

    /// <summary>Signed out by the user.</summary>
    public const string SignedOut = "signed-out";

    /// <summary>A rotated token was presented again, so the whole session was revoked.</summary>
    public const string ReuseDetected = "reuse-detected";
}
