using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlexiBase.Models.enums
{
    /// <summary>
    /// État possible d'un Tenant dans le système
    /// </summary>
    public enum TenantStatus
    {
        /// <summary>Création en cours</summary>
        Provisioning = 0,

        /// <summary>Actif et opérationnel</summary>
        Active = 1,

        /// <summary>En maintenance technique</summary>
        Maintenance = 2,

        /// <summary>Suspendu (accès limité)</summary>
        Suspended = 3,

        /// <summary>Archivé (données conservées)</summary>
        Archived = 4,

        /// <summary>Supprimé définitivement</summary>
        Deleted = 5
    }

    
}