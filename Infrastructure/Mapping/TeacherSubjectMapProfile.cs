using Application.ViewModels.TeacherSubjectMap;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class TeacherSubjectMapProfile : Profile
    {
        public TeacherSubjectMapProfile() 
        {
            CreateMap<TeacherSubjectMapRequest, TeacherSubjectMap>();
            CreateMap<TeacherSubjectMap, TeacherSubjectMapResponse>();

        }
    }
}
