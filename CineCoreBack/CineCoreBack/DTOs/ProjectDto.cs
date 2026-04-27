namespace CineCoreBack.DTOs
{
    public class ProjectCreateDto
    {
        public string Title { get; set; } = null!;
        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }
        public List<int> GenreIds { get; set; } = new();
        public List<string> CustomGenres { get; set; } = new();
        public int OwnerId { get; set; }
    }

    // DTO для оновлення проекту (без OwnerId — власника не змінюємо)
    public class ProjectUpdateDto
    {
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
    }

    public class CrewDto
    {
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}