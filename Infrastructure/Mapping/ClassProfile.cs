using Application.ViewModels.Branch;
using Application.ViewModels.Class;
using AutoMapper;
using Infrastructure;
using SixLabors.ImageSharp.ColorSpaces.Companding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public  class ClassProfile : Profile
    {
        public ClassProfile()
        {
            CreateMap<Class, ClassResponse>();

            CreateMap<ClassRequest, Class>();
        }
    }
}



