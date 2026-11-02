using AutoMapper;
using Application.ViewModels.Branch;

namespace Infrastructure.Mapping
{
    public class BranchProfile : Profile
    {
        public BranchProfile()
        {
            // DTOs keep the original API field name "Schooluid" so existing clients keep working.
            CreateMap<Branch, BranchResponse>()
                .ForMember(d => d.Schooluid, o => o.MapFrom(s => s.SchoolUid));

            CreateMap<BranchRequest, Branch>()
                .ForMember(d => d.SchoolUid, o => o.MapFrom(s => s.Schooluid));
        }
    }
}
