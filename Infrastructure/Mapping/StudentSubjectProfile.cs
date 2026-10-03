using Application.ViewModels.StudentSubject;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class StudentSubjectProfile : Profile
    {
        public StudentSubjectProfile()
        {
            CreateMap<StudentSubject, StudentSubjectResponse>();
            CreateMap<StudentSubjectRequest, StudentSubject>();
        }
    }
}