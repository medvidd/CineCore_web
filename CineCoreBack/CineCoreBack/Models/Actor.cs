using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Actor
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public DateOnly? BirthDate { get; set; }

    public string? PhoneNum { get; set; }

    public string? Email { get; set; }

    public string? Characteristics { get; set; }

    public virtual ICollection<Casting> Castings { get; set; } = new List<Casting>();
}
