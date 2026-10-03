using Application.ViewModels.Subject;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class SubjectProfile : Profile
    {
        public SubjectProfile() {
            CreateMap<Subject, SubjectResponse>();
            CreateMap<SubjectRequest, Subject>();

        }
    }
}
