using System.Collections.Generic;

namespace wspolpracujmy.Models
{
    /// <summary>
    /// Skrócone informacje o grupie do wyświetlania w listach.
    /// </summary>
    public class GroupSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<MemberSummaryDto> Members { get; set; } = new List<MemberSummaryDto>();
    }
}
