namespace FlexiBase.Models.enums
{
    /// <summary>
    /// Niveau d'isolation des données du Tenant
    /// </summary>
    public enum TenantIsolationLevel
    {
        /// <summary>Isolation logique par identifiant</summary>
        Logical = 0,

        /// <summary>Isolation physique partielle (schéma/schema)</summary>
        Schema = 1,

        /// <summary>Isolation physique complète (base dédiée)</summary>
        Database = 2,

        /// <summary>Isolation totale (cluster/serveur dédié)</summary>
        Cluster = 3
    }
}
