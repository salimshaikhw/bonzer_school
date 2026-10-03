using Application.ViewModels.Medium;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class MediumProfile : Profile
    {
        public MediumProfile() {
            CreateMap<Medium, MediumResponse>();
            CreateMap<MediumRequest, Medium>();


        }
    }
}
