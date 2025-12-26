using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Historique des modifications d'un Content Type
    /// </summary>
    [Table("CustomEntityHistories")]
    public class CustomEntityHistory
    {
        /// <summary>
        /// Clé primaire auto-générée par la base de données (identity/auto-increment).
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// Clé étrangère obligatoire vers le CustomEntity associé. Ne peut pas être nulle.
        /// </summary>
        [Required]
        public int CustomEntityId { get; set; }

        /// <summary>
        /// Numéro de version du schéma obligatoire. Correspond à CustomEntity.SchemaVersion.
        /// </summary>
        [Required]
        public int SchemaVersion { get; set; }

        /// <summary>
        /// Description textuelle obligatoire de maximum 1000 caractères.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Détails supplémentaires optionnels (pas de contrainte de longueur).
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Date et heure du changement obligatoire.
        /// </summary>
        [Required]
        public DateTime ChangedAt { get; set; }

        /// <summary>
        /// Identifiant de l'utilisateur obligatoire (max 100 caractères).
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ChangedBy { get; set; } = string.Empty;

        /// <summary>
        /// Propriété de navigation vers l'entité parente (CustomEntity). virtual permet le lazy loading.
        /// </summary>
        [ForeignKey(nameof(CustomEntityId))]
        public virtual CustomEntity CustomEntity { get; set; } = null!;
    }
}
