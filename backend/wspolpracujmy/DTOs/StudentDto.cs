namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// Podstawowe dane studenta do odczytu.
    /// </summary>
    public class StudentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? GroupId { get; set; }
        public string Email { get; set; } = string.Empty;
    }
}