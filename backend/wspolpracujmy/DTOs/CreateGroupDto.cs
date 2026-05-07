namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO do tworzenia nowej grupy.
    /// </summary>
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public int LeaderId { get; set; }
    }
}
