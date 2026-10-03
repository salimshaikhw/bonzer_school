using Application.ViewModels.Branch;
using Application.ViewModels.ExamSubject;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ExamSubjectProfile : Profile
    {
        public ExamSubjectProfile()
        {
            CreateMap<ExamSubject, ExamSubjectResponse>();
            CreateMap<ExamSubjectRequest, ExamSubject>();
        }
    }
}
