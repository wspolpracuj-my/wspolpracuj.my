using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace wspolpracujmy.Models
{
    [Table("CalendarEvents")]
    /// <summary>
    /// Reprezentuje zdarzenie kalendarza (termin, spotkanie) w aplikacji.
    /// </summary>
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

        [JsonIgnore]
        public required Group Group { get; set; }
    }
}
