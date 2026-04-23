using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class Genre
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ProjectGenre> ProjectGenres { get; set; } = new List<ProjectGenre>();
}