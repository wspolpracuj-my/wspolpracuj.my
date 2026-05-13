namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Podsumowanie studenta: dane użytkownika + id rekordu studenta.
    /// </summary>
    public class StudentSummaryDto
    {
        public int StudentId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
    }
}
