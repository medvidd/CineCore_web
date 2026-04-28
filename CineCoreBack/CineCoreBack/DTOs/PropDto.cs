using System.ComponentModel.DataAnnotations;

namespace CineCoreBack.DTOs
{
    public class PropCreateUpdateDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Prop name is required")]
        [MaxLength(150, ErrorMessage = "Prop name cannot exceed 150 characters")]
        public string PropName { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public string? AcquisitionType { get; set; }
        [Required]
        public string? PropStatus { get; set; }
        [Required]
        public string? PropType { get; set; }
    }

    public class PropResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Desc { get; set; }
        public string? Category { get; set; }
        public string? Acquisition { get; set; }
        public string? Status { get; set; }
        public int Scenes { get; set; }
    }
}