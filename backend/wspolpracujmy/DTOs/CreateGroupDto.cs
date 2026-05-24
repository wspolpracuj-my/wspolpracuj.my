namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO do tworzenia nowej grupy.
    /// </summary>
    using Newtonsoft.Json;

    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        // Ignore any incoming leaderId from clients — leader is set from the authenticated student
        [JsonIgnore]
        public int? LeaderId { get; set; }
        // Opcjonalny limit członków dla tworzonej grupy. Jeśli nie podano,
        // można użyć domyślnej wartości z powiązanego projektu.
        public int? MaxMembers { get; set; }
    }
}
