using System;

namespace CineCoreBack.Models;

public partial class ProjectInvitation
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Email { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string SysRole { get; set; } = null!; // Наприклад: "manager", "actor", "crew"
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? Message { get; set; }
    public int InvitedById { get; set; }
    public DateTime DateSent { get; set; } = DateTime.UtcNow;
    public Guid Token { get; set; } = Guid.NewGuid();
    public virtual Project Project { get; set; } = null!;
    public virtual User InvitedBy { get; set; } = null!;
}