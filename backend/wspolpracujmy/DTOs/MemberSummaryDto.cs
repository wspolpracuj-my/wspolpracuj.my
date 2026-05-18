using System;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Skrócone informacje o członku grupy do podglądu.
    /// </summary>
    public class MemberSummaryDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }
}