namespace CineCoreBack.DTOs
{
    // Окремий блок тексту (Action, Dialogue, Character тощо)
    public class ScriptBlockDto
    {
        public string Type { get; set; } = null!;
        public string Content { get; set; } = null!;
    }

    // Те, що приходить від Angular при автозбереженні
    public class ScriptSaveDto
    {
        public int ProjectId { get; set; }
        public List<ScriptBlockDto> Blocks { get; set; } = new();
    }

    // Для правої панелі: список персонажів для вибору
    public class ScriptCharacterResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Color { get; set; } = null!;
        public bool IsAuto { get; set; }
    }
}