using Application.ViewModels.ClassTeacherMap;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ClassTeacherMapProfile : Profile
    {
        public ClassTeacherMapProfile()
        {

            CreateMap<ClassTeacherMapRequest, ClassTeacherMap>(); 
            CreateMap<ClassTeacherMap, ClassTeacherMapResponse>();
        }
}
}

