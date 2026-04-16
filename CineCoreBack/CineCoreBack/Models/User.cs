using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class User
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? PhoneNum { get; set; }

    public DateTime? Birthday { get; set; }

    public DateTime? RegisteredAt { get; set; }

    public virtual ICollection<CallSheet> CallSheets { get; set; } = new List<CallSheet>();

    public virtual ICollection<ProjectMember> ProjectMemberInvitedByUsers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<ProjectMember> ProjectMemberUsers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
