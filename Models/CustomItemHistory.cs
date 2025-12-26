using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Historique des modifications d'un Content Item
    /// </summary>
    [Table("CustomItemHistory")]
    public class CustomItemHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CustomItemId { get; set; }

        [Required]
        public int Version { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string? Details { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; }

        [Required]
        [MaxLength(100)]
        public string ChangedBy { get; set; } = string.Empty;

        [ForeignKey(nameof(CustomItemId))]
        public virtual CustomItem CustomItem { get; set; } = null!;
    }
}
