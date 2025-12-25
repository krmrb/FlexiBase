using ERP.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Utilisation des ressources par un Tenant
    /// </summary>
    [Table("TenantResourceUsages")]
    public class TenantResourceUsage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ResourceType { get; set; } = string.Empty;

        [Required]
        public long UsedAmount { get; set; }

        [Required]
        public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    }
}
