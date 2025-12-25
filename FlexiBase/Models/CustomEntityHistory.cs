using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Historique des modifications d'un Content Type
    /// </summary>
    [Table("CustomEntityHistory")]
    public class CustomEntityHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int CustomEntityId { get; set; }

        [Required]
        public int SchemaVersion { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public string? Details { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; }

        [Required]
        [MaxLength(100)]
        public string ChangedBy { get; set; } = string.Empty;

        [ForeignKey(nameof(CustomEntityId))]
        public virtual CustomEntity CustomEntity { get; set; } = null!;
    }
}
