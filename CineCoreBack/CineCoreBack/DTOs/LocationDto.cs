using System.ComponentModel.DataAnnotations;

namespace CineCoreBack.DTOs
{
    public class LocationCreateUpdateDto
    {
        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "Location name is required")]
        [MaxLength(255, ErrorMessage = "Location name cannot exceed 255 characters")]
        public string LocationName { get; set; } = null!;

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(255)]
        public string? Street { get; set; }

        [MaxLength(200)]
        public string? ContactName { get; set; }

        [MaxLength(30)]
        public string? ContactPhone { get; set; }

        [Required]
        public string? LocationType { get; set; }
    }

    public class LocationResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Desc { get; set; }
        public string? City { get; set; }
        public string? Street { get; set; }
        public string? Type { get; set; }
        public string? Manager { get; set; }
        public string? Phone { get; set; }
        public int Usage { get; set; }
    }
}