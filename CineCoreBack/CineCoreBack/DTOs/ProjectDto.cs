namespace CineCoreBack.DTOs
{
    public class ProjectCreateDto
    {
        public string Title { get; set; } = null!;
        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }
        public List<int> GenreIds { get; set; } = new();
        public int OwnerId { get; set; }
    }

    public class ProjectResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Synopsis { get; set; }
        public DateOnly? StartDate { get; set; }
    }
}