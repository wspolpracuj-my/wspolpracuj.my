using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("GroupFiles")]
    /// <summary>
    /// Powiązanie pliku z grupą (plik udostępniony grupie).
    /// </summary>
    public class GroupFile
    {
        [Required]
        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [Column("file_id")]
        public Guid FileId { get; set; }

        [JsonIgnore]
        public required Group Group { get; set; }

        [JsonIgnore]
        public required FileEntity File { get; set; }
    }
}
