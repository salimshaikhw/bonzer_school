using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities.Organization;



/// <summary>Shared lookup: lives in the catalog database, not in the tenant databases.</summary>
[Table("institution_types")]
[Index(nameof(Name), IsUnique = true)]
public partial class InstitutionType : AuditableEntity
{



    [MaxLength(50)]
    public string? Name { get; set; }

}

