using ERP.Core.Models.ContentItems;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Historique des modifications d'un Content Item
    /// </summary>
    [Table("ContentItemHistory")]
    public class ContentItemHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ContentItemId { get; set; }

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

        [ForeignKey(nameof(ContentItemId))]
        public virtual ContentItem ContentItem { get; set; } = null!;
    }
}
