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

    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }

        public string Role { get; set; } = null!; // Тут буде SysRole (для списку)
        public string JobTitle { get; set; } = null!; // ДОДАНО: Тут буде Job Title (для деталей)

        public string Genre { get; set; } = null!;
        public string Director { get; set; } = null!;
        public int TeamSize { get; set; }
        public string Duration { get; set; } = null!;
        public List<CrewDto> Crew { get; set; } = new();
        public bool IsJoined { get; set; }
    }

    // Клас для передачі інформації про учасників команди
    public class CrewDto
    {
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}