using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using FlexiBase.Models.interfaces;
using FlexiBase.Models.enums;

namespace FlexiBase.Models
{
    /// <summary>
    /// Représente le "moule" ou schéma. Définit un type de contenu personnalisable (ex: "Fiche Produit").
    /// Définit la structure d'un type d'objet métier (ex: Produit, Client, Commande)
    /// </summary>
    [Table("CustomEntities")]
    public class CustomEntity : IAuditable, ISoftDeletable
    {
        #region Propriétés d'Identité

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        /// <summary>
        /// Nom d'affichage (ex: "Fiche Produit", "Dossier Client", "Bon de Commande")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Nom système unique (ex: "product", "customer")
        /// </summary>
        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-z][a-z0-9_]*$", ErrorMessage = "Le nom système doit commencer par une lettre et contenir seulement des lettres minuscules, chiffres et underscores")]
        public string SystemName { get; private set; } = string.Empty;

        /// <summary>
        /// Description de la nouvelle entité personnalisée
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; private set; }

        /// <summary>
        /// Icône pour reconnaissance visuelle
        /// </summary>
        [MaxLength(50)]
        public string? Icon { get; private set; }

        /// <summary>
        /// Code couleur d'identification pour différenciation (code hex)
        /// </summary>
        [MaxLength(7)]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Format couleur hex invalide")]
        public string? Color { get; private set; }

        #endregion

        #region Propriétés d'État

        /// <summary>
        /// État de vie du nouveau "Custom Entity" (ex. : "Draft", "Active", "Archived", "Deprecated")
        /// </summary>
        [Required]
        public CustomEntityStatus Status { get; private set; } = CustomEntityStatus.Draft;

        /// <summary>
        /// Indique si ce 'Custom Entity' est système - non modifiable (Ex. : Pour empêche de supprimer "Utilisateur", "Facture" (essentiels), ...)
        /// </summary>
        [Required]
        public bool IsSystem { get; private set; } = false;

        /// <summary>
        /// Version du schéma (incrémenté à chaque modification)
        /// </summary>
        [Required]
        public int SchemaVersion { get; private set; } = 1;

        #endregion

        #region Propriétés de Configuration

        /// <summary>
        /// Indique si ce 'Custom Entity' peut avoir des enfants (hiérarchie)
        /// </summary>
        [Required]
        public bool AllowsHierarchy { get; private set; } = false;

        /// <summary>
        /// Indique si ce 'Custom Entity' peut être utilisé comme relation
        /// </summary>
        [Required]
        public bool IsRelational { get; private set; } = false;

        /// <summary>
        /// Configuration JSON pour options avancées
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Configuration { get; private set; }

        #endregion

        #region Propriétés d'Audit

        [Required]
        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

        [Required]
        public string CreatedBy { get; private set; } = string.Empty;

        public DateTime? UpdatedAt { get; private set; }

        [MaxLength(100)]
        public string? UpdatedBy { get; private set; }

        public DateTime? DeletedAt { get; private set; }

        public string? DeletedBy { get; private set; }

        public bool IsDeleted => DeletedAt.HasValue;

        [Timestamp]
        public byte[]? RowVersion { get; private set; }

        #endregion

        #region Propriétés de Relation

        /// <summary>
        /// Champs définis pour ce 'Custom Entity'
        /// </summary>
        public virtual ICollection<CustomField> Fields { get; private set; } = new List<CustomField>();

        /// <summary>
        /// Relations avec d'autres 'Custom Entity's
        /// Les relations sont stockées dans la collection Relations de chaque CustomEntity (côté source).
        /// Cela signifie qu'une entité peut avoir plusieurs relations sortantes (vers différentes cibles).
        /// </summary>
        public virtual ICollection<CustomEntityRelation> Relations { get; private set; } = new List<CustomEntityRelation>();

        /// <summary>
        /// Historique des modifications du schéma
        /// </summary>
        public virtual ICollection<CustomEntityHistory> History { get; private set; } = new List<CustomEntityHistory>();

        #endregion

        #region Constructeurs

        protected CustomEntity() { }

        /// <summary>
        /// Crée un nouveau 'Custom Entity'
        /// </summary>
        public CustomEntity(
            string name,
            string systemName,
            string createdBy,
            bool isSystem = false)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Le nom est requis", nameof(name));

            if (string.IsNullOrWhiteSpace(systemName))
                throw new ArgumentException("Le nom système est requis", nameof(systemName));

            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("L'utilisateur créateur est requis", nameof(createdBy));

            Name = name.Trim();
            SystemName = systemName.Trim().ToLowerInvariant();
            CreatedBy = createdBy;
            IsSystem = isSystem;
            Status = CustomEntityStatus.Draft;

            AddHistory("Création du 'Custom Entity'");
        }

        #endregion

        #region Méthodes de Gestion du Cycle de Vie

        /// <summary>
        /// Active le 'Custom Entity'
        /// </summary>
        public void Activate(string updatedBy)
        {
            if (Status != CustomEntityStatus.Draft)
                throw new InvalidOperationException($"Impossible d'activer un 'Custom Entity' en état {Status}");

            if (Fields.Count == 0)
                throw new InvalidOperationException("Impossible d'activer un 'Custom Entity' sans champs");

            if (!ValidateSchema(out List<string> errors))
            {
                throw new InvalidOperationException($"Schéma invalide: {string.Join("; ", errors)}");
            }

            Status = CustomEntityStatus.Active;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Activation du 'Custom Entity'");
        }

        /// <summary>
        /// Archive le 'Custom Entity'
        /// </summary>
        public void Archive(string updatedBy, string reason)
        {
            if (Status == CustomEntityStatus.Archived || Status == CustomEntityStatus.Deprecated)
                throw new InvalidOperationException($"Le 'Custom Entity' est déjà en état {Status}");

            Status = CustomEntityStatus.Archived;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory($"Archivage: {reason}");
        }

        /// <summary>
        /// Déprécie le 'Custom Entity' (version obsolète mais maintenue)
        /// </summary>
        public void Deprecate(string updatedBy, string reason)
        {
            if (Status == CustomEntityStatus.Archived || Status == CustomEntityStatus.Deprecated)
                throw new InvalidOperationException($"Le 'Custom Entity' est déjà en état {Status}");

            Status = CustomEntityStatus.Deprecated;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory($"Dépréciation: {reason}");
        }

        /// <summary>
        /// Supprime le 'Custom Entity' (soft delete)
        /// </summary>
        public void Delete(string deletedBy, string reason)
        {
            if (IsSystem)
                throw new InvalidOperationException("Impossible de supprimer un 'Custom Entity' système");

            if (Status == CustomEntityStatus.Active)
                throw new InvalidOperationException("Impossible de supprimer un 'Custom Entity' actif. Archivez-le d'abord.");

            DeletedAt = DateTime.UtcNow;
            DeletedBy = deletedBy;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = deletedBy;

            AddHistory($"Suppression: {reason}");
        }

        /// <summary>
        /// Restaure un 'Custom Entity' supprimé
        /// </summary>
        public void Restore(string updatedBy)
        {
            if (!IsDeleted)
                throw new InvalidOperationException("Le 'Custom Entity' n'est pas supprimé");

            DeletedAt = null;
            DeletedBy = null;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Restauration du 'Custom Entity'");
        }

        #endregion

        #region Méthodes de Gestion des Champs

        /// <summary>
        /// Ajoute un nouveau champ au 'Custom Entity'
        /// </summary>
        public CustomField AddField(
            string name,
            string systemName,
            FieldDataType dataType,
            string? description = null,
            bool isRequired = false,
            string? defaultValue = null,
            string? configuration = null)
        {
            ValidateFieldName(systemName);

            var order = Fields.Count + 1;

            var field = new CustomField(
                this,
                name,
                systemName,
                dataType,
                order,
                isRequired,
                description,
                defaultValue,
                configuration);

            Fields.Add(field);
            IncrementSchemaVersion();
            UpdatedAt = DateTime.UtcNow;

            AddHistory($"Ajout du champ '{name}' ({systemName})");

            return field;
        }

        /// <summary>
        /// Supprime un champ du 'Custom Entity'
        /// </summary>
        public void RemoveField(string systemName, string updatedBy)
        {
            var field = Fields.FirstOrDefault(f => f.SystemName == systemName);

            if (field == null)
                throw new ArgumentException($"Champ '{systemName}' introuvable", nameof(systemName));

            if (field.IsSystem)
                throw new InvalidOperationException("Impossible de supprimer un champ système");

            Fields.Remove(field);
            ReorderFields();
            IncrementSchemaVersion();
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory($"Suppression du champ '{field.Name}' ({systemName})");
        }

        /// <summary>
        /// Met à jour l'ordre des champs
        /// </summary>
        public void ReorderFields(List<string> orderedSystemNames)
        {
            if (orderedSystemNames.Count != Fields.Count)
                throw new ArgumentException("La liste doit contenir tous les champs", nameof(orderedSystemNames));

            // Mettre à jour l'ordre de chaque champ
            foreach (var field in Fields)
            {
                var newOrder = orderedSystemNames.IndexOf(field.SystemName) + 1;
                if (newOrder > 0)
                {
                    field.UpdateOrder(newOrder);
                }
            }

            // Au lieu de réassigner 'Fields', on crée une liste triée
            //    et on l'utilise pour réaffecter les ordres si nécessaire.
            //    Entity Framework continue de suivre la même collection.
            var orderedFields = Fields.OrderBy(f => f.Order).ToList();

            // S'assurer que l'ordre est séquentiel (1, 2, 3...)
            for (int i = 0; i < orderedFields.Count; i++)
            {
                if (orderedFields[i].Order != i + 1)
                {
                    orderedFields[i].UpdateOrder(i + 1);
                }
            }

            IncrementSchemaVersion();
            UpdatedAt = DateTime.UtcNow;

            AddHistory("Réorganisation des champs");
        }

        /// <summary>
        /// Valide l'intégrité du schéma.
        /// Vérifie qu'une relation ne pointe pas vers elle-même (c'est-à-dire que SourceCustomEntity et TargetCustomEntity ne sont pas les mêmes).
        /// C'est une validation simple pour éviter les relations circulaires directes.
        /// </summary>
        // @TODO: Devrait être appelée avant toute activation (Activate()) et peut-être avant certaines modifications structurelles (comme AddField, RemoveField).
        public bool ValidateSchema(out List<string> errors)
        {
            errors = new List<string>();

            // Vérifier les noms système uniques
            var duplicateSystemNames = Fields
                .GroupBy(f => f.SystemName)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateSystemNames.Any())
            {
                errors.Add($"Noms système dupliqués: {string.Join(", ", duplicateSystemNames)}");
            }

            // Vérifier les champs requis
            foreach (var field in Fields.Where(f => f.IsRequired))
            {
                if (field.DataType == FieldDataType.Relation)
                {
                    var config = field.ParseConfiguration<RelationFieldConfig>();
                    if (config?.RequiredRelationId == null)
                    {
                        errors.Add($"Le champ relation requis '{field.Name}' doit spécifier une relation");
                    }
                }
            }

            // Vérifier les relations
            foreach (var relation in Relations)
            {
                if (relation.TargetCustomEntityId == Id)
                {
                    errors.Add($"Relation circulaire détectée: le 'Custom Entity' ne peut pas être lié à lui-même");
                }
            }

            return errors.Count == 0;
        }

        #endregion

        #region Méthodes de Gestion des Relations

        /// <summary>
        /// Ajoute une relation avec un autre 'Custom Entity'
        /// </summary>
        public CustomEntityRelation AddRelation(
            CustomEntity targetCustomEntity,
            string name,
            string systemName,
            RelationType relationType,
            string? description = null,
            bool isRequired = false)
        {
            if (targetCustomEntity == null)
                throw new ArgumentNullException(nameof(targetCustomEntity));

            // Crée une nouvelle instance de CustomEntityRelation en lui passant this (l'entité source) et l'entité cible.
            var relation = new CustomEntityRelation(
                this,
                targetCustomEntity,
                name,
                systemName,
                relationType,
                isRequired,
                description);

            // Ajoute cette instance à la collection Relations de l'entité source.
            Relations.Add(relation);

            // Met à jour le flag IsRelational à true (car cette entité a maintenant au moins une relation).
            IsRelational = true;

            // Incrémente la version du schéma et ajoute une entrée d'historique.
            IncrementSchemaVersion();

            UpdatedAt = DateTime.UtcNow;

            AddHistory($"Ajout de la relation '{name}' vers {targetCustomEntity.Name}");

            return relation;
        }

        /// <summary>
        /// Obtient toutes les relations disponibles pour ce 'Custom Entity'.
        /// Retourne uniquement les relations dont l'entité cible est active.
        /// Cela permet de s'assurer que l'on ne propose pas de créer des liens vers des entités qui ne sont pas en état Active.
        /// </summary>
        // @TODO: Pas encore intégrée à la logique d'interface utilisateur ou de validation. Elle sera probablement utilisée lors de la création des formulaires dynamiques pour lier des CustomItem.
        public List<CustomEntityRelation> GetAvailableRelations()
        {
            return Relations
                .Where(r => r.SourceCustomEntityId == Id && r.TargetCustomEntity.Status == CustomEntityStatus.Active)
                .ToList();
        }

        #endregion

        #region Méthodes de Configuration

        /// <summary>
        /// Met à jour les informations de base (Name, Description, Icon, Color) sans avoir à appeler 4 setters différents
        /// </summary>
        public void UpdateInfo(
            string? name = null,
            string? description = null,
            string? icon = null,
            string? color = null,
            string updatedBy = "")
        {
            bool changed = false;

            if (!string.IsNullOrWhiteSpace(name) && Name != name)
            {
                Name = name.Trim();
                changed = true;
            }

            if (Description != description)
            {
                Description = description;
                changed = true;
            }

            if (Icon != icon)
            {
                Icon = icon;
                changed = true;
            }

            if (Color != color)
            {
                Color = color;
                changed = true;
            }

            if (changed)
            {
                UpdatedAt = DateTime.UtcNow;
                UpdatedBy = updatedBy;

                var changes = new List<string>();
                if (name != null) changes.Add($"Nom: {name}");
                if (description != null) changes.Add("Description mise à jour");
                if (icon != null) changes.Add($"Icône: {icon}");
                if (color != null) changes.Add($"Couleur: {color}");

                AddHistory($"Modification: {string.Join(", ", changes)}");
            }
        }

        /// <summary>
        /// Active/désactive la hiérarchie
        /// </summary>
        // @TODO: trop minimal. à développer.
        public void SetHierarchy(bool allowsHierarchy, string updatedBy)
        {
            if (AllowsHierarchy != allowsHierarchy)
            {
                AllowsHierarchy = allowsHierarchy;
                UpdatedAt = DateTime.UtcNow;
                UpdatedBy = updatedBy;

                AddHistory($"Hiérarchie {(allowsHierarchy ? "activée" : "désactivée")}");
            }
        }

        /// <summary>
        /// Met à jour la configuration JSON
        /// </summary>
        public void UpdateConfiguration(string configuration, string updatedBy)
        {
            Configuration = configuration;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;

            AddHistory("Configuration mise à jour");
        }

        #endregion

        #region Méthodes Helper Privées

        private void ValidateFieldName(string systemName)
        {
            if (string.IsNullOrWhiteSpace(systemName))
                throw new ArgumentException("Le nom système du champ est requis", nameof(systemName));

            if (!System.Text.RegularExpressions.Regex.IsMatch(systemName, @"^[a-z][a-z0-9_]*$"))
                throw new ArgumentException("Le nom système du champ doit commencer par une lettre et contenir seulement des lettres minuscules, chiffres et underscores", nameof(systemName));

            if (Fields.Any(f => f.SystemName == systemName))
                throw new ArgumentException($"Un champ avec le nom système '{systemName}' existe déjà", nameof(systemName));

            // Noms réservés
            var reservedNames = new[] { "id", "tenantid", "createdat", "createdby", "updatedat", "updatedby", "deletedat" };
            if (reservedNames.Contains(systemName.ToLowerInvariant()))
                throw new ArgumentException($"Le nom système '{systemName}' est réservé", nameof(systemName));
        }

        private void ReorderFields()
        {
            int order = 1;
            foreach (var field in Fields.OrderBy(f => f.Order))
            {
                field.UpdateOrder(order++);
            }
        }

        private void IncrementSchemaVersion()
        {
            SchemaVersion++;
        }

        private void AddHistory(string description)
        {
            History.Add(new CustomEntityHistory
            {
                CustomEntity = this,
                SchemaVersion = SchemaVersion,
                Description = description,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = UpdatedBy ?? CreatedBy
            });
        }

        #endregion

        #region Méthodes d'Utilitaire

        /// <summary>
        /// Vérifie si le 'Custom Entity' peut être modifié. Elle Elle retourne true seulement si :
        /// 1) N'est pas système (!IsSystem)
        /// 2) N'est pas archivé (Status != CustomEntityStatus.Archived)
        /// 3) N'est pas déprécié (Status != CustomEntityStatus.Deprecated)
        /// 4) N'est pas supprimé (!IsDeleted)
        /// </summary>
        public bool CanModify()
        {
            return !IsSystem &&
                   Status != CustomEntityStatus.Archived &&
                   Status != CustomEntityStatus.Deprecated &&
                   !IsDeleted;
        }

        /// <summary>
        /// Obtient tous les champs dans l'ordre d'affichage
        /// </summary>
        public List<CustomField> GetOrderedFields()
        {
            return Fields.OrderBy(f => f.Order).ToList();
        }

        /// <summary>
        /// Obtient un champ par son nom système
        /// </summary>
        public CustomField? GetField(string systemName)
        {
            return Fields.FirstOrDefault(f => f.SystemName == systemName);
        }

        /// <summary>
        /// Clone le 'Custom Entity' (pour les templates)
        /// </summary>
        public CustomEntity Clone(string createdBy, string? newSystemName = null)
        {
            var clone = new CustomEntity(
                Name,
                newSystemName ?? SystemName,
                createdBy,
                false);

            clone.Description = Description;
            clone.Icon = Icon;
            clone.Color = Color;
            clone.AllowsHierarchy = AllowsHierarchy;

            // Cloner les champs
            foreach (var field in GetOrderedFields())
            {
                clone.AddField(
                    field.Name,
                    field.SystemName,
                    field.DataType,
                    field.Description,
                    field.IsRequired,
                    field.DefaultValue,
                    field.Configuration);
            }

            // Note: les relations ne sont pas clonées automatiquement
            // car elles dépendent d'autres 'Custom Entity's

            return clone;
        }

        #endregion

        #region Overrides

        public override string ToString() => $"{Name} ({SystemName}) [v{SchemaVersion}]";

        #endregion
    }
}