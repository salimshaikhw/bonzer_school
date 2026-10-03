using Application.ViewModels.Branch;
using Application.ViewModels.ClassFeeMap;
using AutoMapper;
using Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ClassFeeMapProfile : Profile
    {
        public ClassFeeMapProfile() 
        {

            CreateMap<ClassFeeMapRequest, ClassFeeMap>();
            CreateMap<ClassFeeMap, ClassFeeMapResponse>();


        }
    }
}





//// Map from database scaffolded entity (snake_case) to view model (PascalCase)
//CreateMap<branch, BranchResponse>()
//    .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.id))
//    .ForMember(dest => dest.GroupId, opt => opt.MapFrom(src => src.group_id))
//    .ForMember(dest => dest.InstitutionTypeId, opt => opt.MapFrom(src => src.institution_type_id))
//    .ForMember(dest => dest.MediumId, opt => opt.MapFrom(src => src.medium_id))
//    .ForMember(dest => dest.Schooluid, opt => opt.MapFrom(src => src.schooluid))
//    .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.name))
//    .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.address))
//    .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.city))
//    .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.state))
//    .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.phone))
//    .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.email))
//    .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.created_at));

//// Map from request view model to database entity (used on create/update)
//CreateMap<BranchRequest, branch>()
//    .ForMember(dest => dest.group_id, opt => opt.MapFrom(src => src.GroupId))
//    .ForMember(dest => dest.institution_type_id, opt => opt.MapFrom(src => src.InstitutionTypeId))
//    .ForMember(dest => dest.medium_id, opt => opt.MapFrom(src => src.MediumId))
//    .ForMember(dest => dest.schooluid, opt => opt.MapFrom(src => src.Schooluid))
//    .ForMember(dest => dest.name, opt => opt.MapFrom(src => src.Name))
//    .ForMember(dest => dest.address, opt => opt.MapFrom(src => src.Address))
//    .ForMember(dest => dest.city, opt => opt.MapFrom(src => src.City))
//    .ForMember(dest => dest.state, opt => opt.MapFrom(src => src.State))
//    .ForMember(dest => dest.phone, opt => opt.MapFrom(src => src.Phone))
//    .ForMember(dest => dest.email, opt => opt.MapFrom(src => src.Email))
//    .ForMember(dest => dest.created_at, opt => opt.MapFrom(src => src.CreatedAt))
//    .ForMember(dest => dest.id, opt => opt.Ignore());
//        }