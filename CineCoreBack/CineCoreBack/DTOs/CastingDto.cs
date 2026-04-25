using System;

namespace CineCoreBack.DTOs;

// ==========================================
// DTO ДЛЯ РОЛЕЙ
// ==========================================
public class RoleDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public int? Age { get; set; }
    public string? Characteristics { get; set; } // JSON
    public string RoleType { get; set; } = null!;
    public string ColorHex { get; set; } = null!;
    public int CandidatesCount { get; set; }
}

public class CreateRoleDto
{
    public string RoleName { get; set; } = null!;
    public string? Description { get; set; }
    public int? Age { get; set; }
    public string? Characteristics { get; set; } // JSON
    public string RoleType { get; set; } = "supporting";
    public string? ColorHex { get; set; }
}

public class UpdateRoleDto : CreateRoleDto { }

// ==========================================
// DTO ДЛЯ КАНДИДАТІВ (CASTING)
// ==========================================
public class CandidateDto
{
    public int ActorId { get; set; } // Це User.Id

    // Дані з таблиці User
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNum { get; set; }
    public string AvatarTheme { get; set; } = null!;

    // Дані з таблиці Actor
    public string? Characteristics { get; set; } // JSON з параметрами актора

    // Дані з таблиці Casting
    public string CastStatus { get; set; } = null!;
    public DateOnly CastDate { get; set; }
    public string? Notes { get; set; }
}

public class AddCandidateDto
{
    public int ActorId { get; set; }
    public string? Notes { get; set; }
}

public class UpdateCastStatusDto
{
    public string CastStatus { get; set; } = null!;
}

// ==========================================
// DTO ДЛЯ ОСОБИСТОГО ПРОФІЛЮ АКТАРА
// ==========================================
public class ActorProfileDto
{
    public int Id { get; set; } // User.Id
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNum { get; set; }
    public DateOnly? Birthday { get; set; }
    public string AvatarTheme { get; set; } = null!;
    public string? Characteristics { get; set; } // JSON
}

public class UpdateActorCharacteristicsDto
{
    public string Characteristics { get; set; } = "{}";
}