using System.ComponentModel.DataAnnotations;

namespace CineCoreBack.DTOs
{
    public class ProjectCreateDto
    {
        [Required(ErrorMessage = "Project title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = null!;

        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }
        public List<int> GenreIds { get; set; } = new();
        public List<string> CustomGenres { get; set; } = new();

        [Required]
        public int OwnerId { get; set; }
    }

    public class ProjectUpdateDto
    {
        [Required(ErrorMessage = "Project title is required")]
        [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        public string Title { get; set; } = null!;

        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }
        public List<int> GenreIds { get; set; } = new();
        public List<string> CustomGenres { get; set; } = new();
    }

    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }

        public string Role { get; set; } = null!;
        public string JobTitle { get; set; } = null!;

        public string Genre { get; set; } = null!;
        public string Director { get; set; } = null!;
        public int TeamSize { get; set; }
        public string Duration { get; set; } = null!;
        public List<CrewDto> Crew { get; set; } = new();
        public bool IsJoined { get; set; }
        public List<int> GenreIds { get; set; } = new();
    }

    public class CrewDto
    {
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}