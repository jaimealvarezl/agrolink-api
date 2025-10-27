namespace AgroLink.Core.DTOs;

public class FarmDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaddockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int FarmId { get; set; }
    public string FarmName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LotDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int PaddockId { get; set; }
    public string PaddockName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class OwnerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
    public DateTime ExpiresAt { get; set; }
}

// Request DTOs for AuthController
public class RegisterRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

// Create DTOs
public class CreateFarmDto
{
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
}

public class CreatePaddockDto
{
    public string Name { get; set; } = string.Empty;
    public int FarmId { get; set; }
}

public class CreateLotDto
{
    public string Name { get; set; } = string.Empty;
    public int PaddockId { get; set; }
    public string? Status { get; set; }
}

// Update DTOs
public class UpdateFarmDto
{
    public string? Name { get; set; }
    public string? Location { get; set; }
}

public class UpdatePaddockDto
{
    public string? Name { get; set; }
    public int? FarmId { get; set; }
}

public class UpdateLotDto
{
    public string? Name { get; set; }
    public int? PaddockId { get; set; }
    public string? Status { get; set; }
}
