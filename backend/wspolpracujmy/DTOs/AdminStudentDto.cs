namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO listy studentów dla widoku administratora.
    /// </summary>
    public class AdminStudentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
    }
}
