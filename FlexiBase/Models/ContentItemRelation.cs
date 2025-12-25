using ERP.Core.Models.ContentItems;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Relation entre Content Items
    /// </summary>
    [Table("ContentItemRelations")]
    public class ContentItemRelation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SourceItemId { get; set; }

        [Required]
        public int RelatedItemId { get; set; }

        [Required]
        [MaxLength(50)]
        public string RelationType { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SourceItemId))]
        public virtual ContentItem SourceItem { get; set; } = null!;

        [ForeignKey(nameof(RelatedItemId))]
        public virtual ContentItem RelatedItem { get; set; } = null!;

        public ContentItemRelation() { }

        public ContentItemRelation(
            ContentItem sourceItem,
            ContentItem relatedItem,
            string relationType,
            string? metadata = null)
        {
            SourceItem = sourceItem;
            SourceItemId = sourceItem.Id;
            RelatedItem = relatedItem;
            RelatedItemId = relatedItem.Id;
            RelationType = relationType;
            Metadata = metadata;
        }
    }
}
