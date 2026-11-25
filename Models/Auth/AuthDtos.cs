namespace DatasoftApiSample.Models.Auth;

/// <summary>Giriş isteği. Swagger: AuthorizationDto</summary>
public sealed class LoginRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }

    /// <summary>Çalışılacak şirket. Boşsa kullanıcının varsayılan şirketi kullanılır.</summary>
    public long? CompanyId { get; set; }
}

/// <summary>Token yenileme isteği. Swagger: RTokenDto</summary>
public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
    public long UserId { get; set; }
}

/// <summary>Çıkış isteği. Swagger: LogOutDto</summary>
public sealed class LogoutRequest
{
    public bool IsAdmin { get; set; }
    public string? RefreshToken { get; set; }
    public long UserId { get; set; }
}

/// <summary>Giriş ve token yenileme yanıtı. Swagger: CredentialsDto</summary>
public sealed class CredentialsDto
{
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public long CompanyId { get; set; }
    public string? CompanyName { get; set; }

    /// <summary>Ön muhasebe çağrılarında "Authorization: Bearer ..." başlığında gönderilir.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Access token'ı şifre göndermeden yenilemek için kullanılır.</summary>
    public string? RefreshToken { get; set; }

    /// <summary>true ise iki adımlı doğrulama bekleniyordur ve token dönmez.</summary>
    public bool TwoFactorRequired { get; set; }

    /// <summary>true ise şirketin şubeleri vardır ve girişte şube seçilmelidir.</summary>
    public bool RequiredBranchChoice { get; set; }

    /// <summary>true ise girişte dönem seçilmelidir.</summary>
    public bool RequiredPeriodChoice { get; set; }
}
