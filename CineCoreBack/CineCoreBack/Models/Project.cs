using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Project
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string? Synopsis { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int OwnerId { get; set; }

    public virtual User Owner { get; set; } = null!;

    public virtual ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();

    public virtual ICollection<Scene> Scenes { get; set; } = new List<Scene>();

    public virtual ICollection<ShootDay> ShootDays { get; set; } = new List<ShootDay>();

    public virtual ICollection<ProjectGenre> ProjectGenres { get; set; } = new List<ProjectGenre>();
    public virtual ICollection<Resource> Resources { get; set; } = new List<Resource>();
    public virtual ICollection<ProjectInvitation> ProjectInvitations { get; set; } = new List<ProjectInvitation>();
}
