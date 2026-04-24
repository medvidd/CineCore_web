namespace CineCoreBack.DTOs
{
    // DTO для пошуку користувача по email під час вводу
    public class UserSearchResultDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    // DTO для відправки запрошення з фронтенду
    public class CreateInvitationDto
    {
        public int ProjectId { get; set; }
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string SysRole { get; set; } = null!;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Message { get; set; }
        public int InvitedById { get; set; } // ID того, хто зараз авторизований і робить запрошення
    }

    // DTO для відображення Pending Invites у таблиці
    public class PendingInvitationResponseDto
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string SysRole { get; set; } = null!;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string InvitedBy { get; set; } = null!; // Ім'я того, хто запросив
        public DateTime DateSent { get; set; }
    }

    // DTO для відображення Active Members у таблиці
    public class ActiveMemberResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string SysRole { get; set; } = null!;
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public DateTime JoinedDate { get; set; }
    }

    // DTO для редагування даних учасника проекту
    public class UpdateMemberDto
    {
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string SysRole { get; set; } = null!;
    }
}