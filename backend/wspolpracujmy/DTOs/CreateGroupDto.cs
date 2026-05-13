namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO do tworzenia nowej grupy.
    /// </summary>
    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public int LeaderId { get; set; }
        // Opcjonalny limit członków dla tworzonej grupy. Jeśli nie podano,
        // można użyć domyślnej wartości z powiązanego projektu.
        public int? MaxMembers { get; set; }
    }
}
