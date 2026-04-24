namespace CineCoreBack.DTOs
{
    public class PropCreateUpdateDto
    {
        public string PropName { get; set; } = null!;
        public string? Description { get; set; }
        public string? AcquisitionType { get; set; }
        public string? PropStatus { get; set; }
        public string? PropType { get; set; }
    }

    public class PropResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Desc { get; set; }
        public string? Category { get; set; } // PropType
        public string? Acquisition { get; set; }
        public string? Status { get; set; }
        public int Scenes { get; set; } // Кількість сцен
    }
}