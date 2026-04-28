using System.ComponentModel.DataAnnotations;

namespace CineCoreBack.DTOs
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? AvatarTheme { get; set; }
        public string? PhoneNum { get; set; }
        public DateTime? Birthday { get; set; }
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? PhoneNum { get; set; }
        public string? AvatarTheme { get; set; }
    }

    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }

    public class UserRegisterDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "First Name is required")]
        [MaxLength(50)]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name is required")]
        [MaxLength(50)]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format")]
        public string? PhoneNum { get; set; }

        [Required(ErrorMessage = "Birthday is required")]
        public DateTime? Birthday { get; set; }
        public string? AvatarTheme { get; set; }
    }

    public class UserUpdateDto
    {
        [Required] public string FirstName { get; set; } = null!;
        [Required] public string LastName { get; set; } = null!;
        [Required]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
        [RegularExpression(@"^\+?[1-9]\d{1,14}$")] public string? PhoneNum { get; set; }
        public string? AvatarTheme { get; set; }
    }

    public class UserPasswordUpdateDto
    {
        [Required] public string CurrentPassword { get; set; } = null!;
        [Required][MinLength(8)] public string NewPassword { get; set; } = null!;
    }
}
