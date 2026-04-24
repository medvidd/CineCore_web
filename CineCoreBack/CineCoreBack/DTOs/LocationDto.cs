namespace CineCoreBack.DTOs
{
    public class LocationCreateUpdateDto
    {
        public string LocationName { get; set; } = null!;
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? ContactName { get; set; }
        public string? ContactPhone { get; set; }
        public string? LocationType { get; set; }
    }

    public class LocationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Desc { get; set; } // Комбінація City + Street для фронтенду
        public string? Type { get; set; }
        public string? Manager { get; set; }
        public string? Phone { get; set; }
        public int Usage { get; set; } // Кількість сцен, де задіяна локація
    }
}