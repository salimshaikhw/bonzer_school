using Application.ViewModels.Branch;
using Application.ViewModels.ClassSubjectMap;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ClassSubjectMapProfile : Profile
    {
        public ClassSubjectMapProfile()
        {

            CreateMap<ClassSubjectMap, ClassSubjectMapResponse>();

            // Map from request view model to database entity (used on create/update)
            CreateMap<ClassSubjectMapRequest, ClassSubjectMap>();
        }
    }
}
