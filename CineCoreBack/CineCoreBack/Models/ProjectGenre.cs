using System;
using System.Collections.Generic;

namespace CineCoreBack.Models;

public partial class ProjectGenre
{
    public int ProjectId { get; set; }

    public int GenreId { get; set; }

    public virtual Genre Genre { get; set; } = null!;

    public virtual Project Project { get; set; } = null!;
}