using FlexiBase.Models.enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace FlexiBase.Models
{
    /// <summary>
    /// Représente un champ dans un Content Type
    /// </summary>
    [Table("ContentFields")]
    public class ContentField
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; private set; }

        [Required]
        public int CustomEntityId { get; private set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; private set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [RegularExpression(@"^[a-z][a-z0-9_]*$")]
        public string SystemName { get; private set; } = string.Empty;

        [Required]
        public FieldDataType DataType { get; private set; }

        [MaxLength(500)]
        public string? Description { get; private set; }

        [Required]
        public int Order { get; private set; } = 1;

        [Required]
        public bool IsRequired { get; private set; } = false;

        [Required]
        public bool IsSystem { get; private set; } = false;

        [Required]
        public bool IsSearchable { get; private set; } = true;

        [Required]
        public bool IsFilterable { get; private set; } = true;

        [MaxLength(1000)]
        public string? DefaultValue { get; private set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Configuration { get; private set; }

        public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; private set; }

        [ForeignKey(nameof(CustomEntityId))]
        public virtual CustomEntity CustomEntity { get; private set; } = null!;

        /// <summary>
        /// Règles de validation au format JSON
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ValidationRules { get; private set; }

        protected ContentField() { }

        public ContentField(
            CustomEntity customEntity,
            string name,
            string systemName,
            FieldDataType dataType,
            int order,
            bool isRequired = false,
            string? description = null,
            string? defaultValue = null,
            string? configuration = null,
            bool isSystem = false)
        {
            CustomEntity = customEntity ?? throw new ArgumentNullException(nameof(customEntity));
            CustomEntityId = customEntity.Id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            SystemName = systemName ?? throw new ArgumentNullException(nameof(systemName));
            DataType = dataType;
            Order = order;
            IsRequired = isRequired;
            Description = description;
            DefaultValue = defaultValue;
            Configuration = configuration;
            IsSystem = isSystem;
        }

        #region Méthodes de Validation
        /// <summary>
        /// Valide une valeur pour ce champ
        /// </summary>
        public bool ValidateValue(object? value, out List<string> errors)
        {
            errors = new List<string>();

            // 1. Validation champ requis
            if (IsRequired && IsValueEmpty(value))
            {
                errors.Add($"Le champ '{Name}' est obligatoire");
                return false;
            }

            // Si valeur null et non requis, OK
            if (value == null)
                return true;

            // 2. Validation par type de données
            errors.AddRange(ValidateByDataType(value));

            // 3. Validation règles personnalisées
            errors.AddRange(ValidateCustomRules(value));

            return errors.Count == 0;
        }

        /// <summary>
        /// Vérifie si une valeur est vide selon son type
        /// </summary>
        private bool IsValueEmpty(object? value)
        {
            if (value == null) return true;

            switch (DataType)
            {
                case FieldDataType.String:
                case FieldDataType.Text:
                case FieldDataType.Email:
                case FieldDataType.Url:
                case FieldDataType.Phone:
                case FieldDataType.Color:
                case FieldDataType.Select:
                    return string.IsNullOrWhiteSpace(value.ToString());

                case FieldDataType.Integer:
                    return Convert.ToInt64(value) == 0;

                case FieldDataType.Decimal:
                    return Convert.ToDecimal(value) == 0;

                case FieldDataType.Date:
                case FieldDataType.DateTime:
                    return Convert.ToDateTime(value) == DateTime.MinValue;

                case FieldDataType.Time:
                    return string.IsNullOrWhiteSpace(value.ToString());

                case FieldDataType.File:
                case FieldDataType.Image:
                    return string.IsNullOrWhiteSpace(value.ToString());

                case FieldDataType.Relation:
                    return Convert.ToInt32(value) == 0;

                case FieldDataType.MultiSelect:
                case FieldDataType.Json:
                    return string.IsNullOrWhiteSpace(value.ToString());

                default:
                    return value == null;
            }
        }

        /// <summary>
        /// Valide selon le type de données
        /// </summary>
        private List<string> ValidateByDataType(object value)
        {
            var errors = new List<string>();

            try
            {
                switch (DataType)
                {
                    case FieldDataType.Email:
                        if (!Regex.IsMatch(value.ToString()!, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                            errors.Add($"L'email '{value}' n'est pas valide");
                        break;

                    case FieldDataType.Url:
                        if (!Uri.TryCreate(value.ToString(), UriKind.Absolute, out _))
                            errors.Add($"L'URL '{value}' n'est pas valide");
                        break;

                    case FieldDataType.Integer:
                        if (!long.TryParse(value.ToString(), out _))
                            errors.Add($"La valeur '{value}' n'est pas un entier valide");
                        break;

                    case FieldDataType.Decimal:
                        if (!decimal.TryParse(value.ToString(), out _))
                            errors.Add($"La valeur '{value}' n'est pas un nombre décimal valide");
                        break;

                    case FieldDataType.Date:
                    case FieldDataType.DateTime:
                        if (!DateTime.TryParse(value.ToString(), out _))
                            errors.Add($"La valeur '{value}' n'est pas une date valide");
                        break;

                    case FieldDataType.Time:
                        if (!TimeSpan.TryParse(value.ToString(), out _))
                            errors.Add($"La valeur '{value}' n'est pas une heure valide");
                        break;

                    case FieldDataType.Color:
                        var color = value.ToString()!;
                        if (!Regex.IsMatch(color, @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$"))
                            errors.Add($"La couleur '{value}' n'est pas valide (format hex: #RRGGBB)");
                        break;
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Erreur de validation: {ex.Message}");
            }

            return errors;
        }

        /// <summary>
        /// Valide selon les règles personnalisées
        /// </summary>
        private List<string> ValidateCustomRules(object value)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ValidationRules))
                return errors;

            try
            {
                // Si vous avez des règles de validation JSON
                // var rules = JsonSerializer.Deserialize<ValidationRules>(ValidationRules);
                // Implémentez la validation ici
            }
            catch
            {
                // Ignorer si les règles sont mal formatées
            }

            return errors;
        }

        #endregion

        #region Méthodes de Conversion

        /// <summary>
        /// Convertit une valeur depuis le stockage
        /// </summary>
        public object? ConvertFromStorage(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
                return null;

            try
            {
                switch (DataType)
                {
                    case FieldDataType.Integer:
                        return long.Parse(storedValue);

                    case FieldDataType.Decimal:
                        return decimal.Parse(storedValue);

                    case FieldDataType.Boolean:
                        return bool.Parse(storedValue);

                    case FieldDataType.Date:
                    case FieldDataType.DateTime:
                        return DateTime.Parse(storedValue);

                    case FieldDataType.Time:
                        return TimeSpan.Parse(storedValue);

                    default:
                        return storedValue;
                }
            }
            catch
            {
                return storedValue;
            }
        }

        /// <summary>
        /// Convertit une valeur vers le stockage
        /// </summary>
        public string? ConvertToStorage(object? value)
        {
            if (value == null)
                return null;

            switch (DataType)
            {
                case FieldDataType.Date:
                    return ((DateTime)value).ToString("yyyy-MM-dd");

                case FieldDataType.DateTime:
                    return ((DateTime)value).ToString("O"); // ISO 8601

                case FieldDataType.Time:
                    return ((TimeSpan)value).ToString();

                default:
                    return value.ToString();
            }
        }

        #endregion

        public void Update(
            string? name = null,
            string? description = null,
            bool? isRequired = null,
            bool? isSearchable = null,
            bool? isFilterable = null,
            string? defaultValue = null,
            string? configuration = null)
        {
            if (!string.IsNullOrWhiteSpace(name))
                Name = name.Trim();

            if (description != null)
                Description = description;

            if (isRequired.HasValue)
                IsRequired = isRequired.Value;

            if (isSearchable.HasValue)
                IsSearchable = isSearchable.Value;

            if (isFilterable.HasValue)
                IsFilterable = isFilterable.Value;

            if (defaultValue != null)
                DefaultValue = defaultValue;

            if (configuration != null)
                Configuration = configuration;

            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateOrder(int order)
        {
            Order = order;
            UpdatedAt = DateTime.UtcNow;
        }

        public T? ParseConfiguration<T>() where T : class
        {
            if (string.IsNullOrWhiteSpace(Configuration))
                return null;

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(Configuration);
            }
            catch
            {
                return null;
            }
        }

        public void SetConfiguration<T>(T config) where T : class
        {
            Configuration = System.Text.Json.JsonSerializer.Serialize(config);
            UpdatedAt = DateTime.UtcNow;
        }

    }
}
