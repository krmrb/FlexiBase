using FlexiBase.Models.enums;

namespace FlexiBase.Models
{
    /// <summary>
    /// Résumé d'un Content Item
    /// </summary>
    public class ContentItemSummary
    {
        public int Id { get; set; }
        public string Uuid { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string CustomEntityName { get; set; } = string.Empty;
        public ContentItemStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
