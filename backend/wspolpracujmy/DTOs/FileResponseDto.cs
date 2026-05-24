using System;

namespace wspolpracujmy.DTOs
{
    /// <summary>
    /// DTO zwracane przez endpointy zarządzania plikami zespołu.
    /// Zawiera metadane pliku bez ujawniania wewnętrznej ścieżki w GCS.
    /// </summary>
    public class FileResponseDto
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public int TeamId { get; set; }
        public DateTime UploadDate { get; set; }
    }
}
