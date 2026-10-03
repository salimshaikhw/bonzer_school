using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using Application.ViewModels.SchoolGroup;

namespace Infrastructure.Mapping
{
    public class SchoolGroupProfile : Profile
    {
        public SchoolGroupProfile()
        {
            CreateMap<SchoolGroup, SchoolGroupDto>();
            CreateMap<CreateSchoolGroupInput, SchoolGroup>();
            CreateMap<UpdateSchoolGroupInput, SchoolGroup>();
        }
    }
}
