using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class ProjectMember
{
    public int ProjectId { get; set; }

    public int? UserId { get; set; }

    public string InvitedEmail { get; set; } = null!;

    public string? JobTitle { get; set; }

    public string? Department { get; set; }

    public int? InvitedByUserId { get; set; }

    public DateTime? InvitedAt { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual User? InvitedByUser { get; set; }

    public virtual Project Project { get; set; } = null!;

    public virtual User? User { get; set; }
}
