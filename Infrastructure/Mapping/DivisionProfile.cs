using Application.ViewModels.Branch;
using Application.ViewModels.Division;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class DivisionProfile : Profile
    {
        public DivisionProfile()
        {
          
            CreateMap<Division, DivisionResponse>();

            CreateMap<DivisionRequest, Division>();
        }
    }
}
