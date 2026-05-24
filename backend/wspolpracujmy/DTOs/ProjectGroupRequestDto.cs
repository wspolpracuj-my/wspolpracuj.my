using System;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Oczekujące zgłoszenie zespołu do projektu (widok firmy).
    /// </summary>
    public class ProjectGroupRequestDto
    {
        public int RequestId { get; set; }
        public int GroupId { get; set; }
        public required string GroupName { get; set; }
        public int MemberCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
