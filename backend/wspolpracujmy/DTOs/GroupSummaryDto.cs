using System.Collections.Generic;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Skrócone informacje o grupie do wyświetlania w listach.
    /// </summary>
    public class GroupSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int LeaderId { get; set; }
        public int? ProjectId { get; set; }
        public int? MaxMembers { get; set; }
        public bool? IsAccepted { get; set; }
        public int MemberCount { get; set; }
        public List<MemberSummaryDto> Members { get; set; } = new List<MemberSummaryDto>();
    }
}