using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace wspolpracujmy.Models
{
    [Table("CalendarEvents")]
    public class CalendarEvent
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("time")]
        public DateTime Time { get; set; }


        [Required]
        [Column("group_id")]
        public int GroupId { get; set; }

        [Required]
        [Column("name")]
        public required string Name { get; set; }

        public required Group Group { get; set; }
    }
}
