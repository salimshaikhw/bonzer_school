using Application.ViewModels.FeeStructure;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class FeeStructureProfile : Profile
    {
        public FeeStructureProfile()
        {
            CreateMap<FeeStructure, FeeStructureResponse>();
            CreateMap<FeeStructureRequest, FeeStructure>();
        }
    }

}
