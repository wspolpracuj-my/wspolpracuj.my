namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Odpowiedź przy tworzeniu nowej grupy.
    /// </summary>
    public class CreateGroupResultDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ProjectId { get; set; }
        public int? LeaderId { get; set; }
        public int MemberCount { get; set; }
    }
}
