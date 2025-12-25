using ERP.Core.Models;
using FlexiBase.Models.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Historique des changements de statut d'un Tenant
    /// </summary>
    [Table("TenantStatusHistory")]
    public class TenantStatusHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int TenantId { get; set; }

        [Required]
        public TenantStatus OldStatus { get; set; }

        [Required]
        public TenantStatus NewStatus { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [Required]
        public DateTime ChangedAt { get; set; }

        [MaxLength(100)]
        public string? ChangedBy { get; set; }
    }
}
