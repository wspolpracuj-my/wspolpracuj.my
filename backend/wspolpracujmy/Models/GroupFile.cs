using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("GroupFiles")]
    public class GroupFile
    {
        [Required]
        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [Column("file_id")]
        public Guid FileId { get; set; }

        public required Group Group { get; set; }
        public required FileEntity File { get; set; }
    }
}
