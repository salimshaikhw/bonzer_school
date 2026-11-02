using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Organization;



/// <summary>Shared lookup: lives in the catalog database, not in the tenant databases.</summary>
[Table("mediums")]
[Index(nameof(Name), IsUnique = true)]
public partial class Medium : AuditableEntity
{



    [MaxLength(50)]
    public string? Name { get; set; }

}

