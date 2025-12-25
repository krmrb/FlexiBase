using ERP.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Configuration personnalisée d'un Tenant
    /// </summary>
    [Table("TenantConfigurations")]
    public class TenantConfiguration
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public int TenantId { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Key { get; private set; } = string.Empty;

        [MaxLength(4000)]
        public string Value { get; private set; } = string.Empty;

        [Required]
        public bool IsSystem { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        // Constructeur pour EF Core
        protected TenantConfiguration() { }

        public TenantConfiguration(string key, string value, bool isSystem = false)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            Value = value;
            IsSystem = isSystem;
        }

        public void Update(string value, bool? isSystem = null)
        {
            Value = value;
            if (isSystem.HasValue) IsSystem = isSystem.Value;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
