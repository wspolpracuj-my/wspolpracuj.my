namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO używany do tworzenia GroupRequest przez API.
    /// Pola:
    /// - GroupId: id grupy docelowej (wymagane).
    /// - ProjectId: id projektu (opcjonalne) — wymagane przy typie "ProjectRequest".
    /// - TargetStudentId: id studenta będącego celem zaproszenia (opcjonalne) — wymagane przy typie "Invitation".
    /// - Type: typ żądania: "Invitation", "ProjectRequest" lub "Application".
    /// Uwaga: `CreatedByUserId` nie jest przekazywane w DTO — pobierane jest z aktualnie zalogowanego użytkownika po stronie serwera.
    /// </summary>
    public class CreateGroupRequestDto
    {
        public int GroupId { get; set; }
        public int? ProjectId { get; set; }
        public int? TargetStudentId { get; set; }
        public string? Type { get; set; }
    }
}
