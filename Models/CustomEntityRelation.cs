using FlexiBase.Models.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models
{
    /// <summary>
    /// Représente une relation entre deux Content Types
    /// </summary>
    [Table("CustomEntityRelations")]
    public class CustomEntityRelation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public int SourceCustomEntityId { get; private set; }

        [Required]
        public int TargetCustomEntityId { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; private set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string SystemName { get; private set; } = string.Empty;

        /// <summary>
        /// Permet de spécifier la cardinalité de la relation (OneToOne, OneToMany, ManyToOne, ManyToMany). 
        /// </summary>
        [Required]
        public RelationType RelationType { get; private set; }

        [MaxLength(500)]
        public string? Description { get; private set; }

        [Required]
        public bool IsRequired { get; private set; } = false;

        [Required]
        public bool IsSystem { get; private set; } = false;

        [Column(TypeName = "nvarchar(max)")]
        public string? Configuration { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        [ForeignKey(nameof(SourceCustomEntityId))]
        public virtual CustomEntity SourceCustomEntity { get; private set; } = null!;

        [ForeignKey(nameof(TargetCustomEntityId))]
        public virtual CustomEntity TargetCustomEntity { get; private set; } = null!;

        protected CustomEntityRelation() { }

        public CustomEntityRelation(
            CustomEntity sourceCustomEntity,
            CustomEntity targetCustomEntity,
            string name,
            string systemName,
            RelationType relationType,
            bool isRequired = false,
            string? description = null,
            bool isSystem = false)
        {
            SourceCustomEntity = sourceCustomEntity ?? throw new ArgumentNullException(nameof(sourceCustomEntity));
            SourceCustomEntityId = sourceCustomEntity.Id;
            TargetCustomEntity = targetCustomEntity ?? throw new ArgumentNullException(nameof(targetCustomEntity));
            TargetCustomEntityId = targetCustomEntity.Id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SystemName = systemName ?? throw new ArgumentNullException(nameof(systemName));
            RelationType = relationType;
            IsRequired = isRequired;
            Description = description;
            IsSystem = isSystem;
        }
    }
}
