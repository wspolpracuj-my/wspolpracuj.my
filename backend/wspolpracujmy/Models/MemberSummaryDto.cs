using System;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// Skrócone informacje o członku grupy do podglądu.
    /// </summary>
    public class MemberSummaryDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
    }
}
