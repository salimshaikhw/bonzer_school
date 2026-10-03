using Application.ViewModels.InstitutionType;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class InstitutionTypeProfile : Profile
    {
        public InstitutionTypeProfile()
        {
            CreateMap<InstitutionType, InstitutionTypeResponse>();
            CreateMap<InstitutionTypeRequest, InstitutionType>();


        }
    }
}
