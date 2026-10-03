using AutoMapper;
using Application.ViewModels.Branch;
using Domain.Entities.Postgres.Scaffolded;

namespace Infrastructure.Mapping
{
    public class BranchProfile : Profile
    {
        public BranchProfile()
        {
            CreateMap<Branch, BranchResponse>();

            CreateMap<BranchRequest, Branch>();
        }
    }
}
