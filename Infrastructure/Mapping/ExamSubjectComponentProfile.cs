using Application.ViewModels.ExamSubject;
using Application.ViewModels.ExamSubjectComponent;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ExamSubjectComponentProfile : Profile
    {
        public ExamSubjectComponentProfile()
        {
            CreateMap<ExamSubjectComponentRequest, ExamSubjectComponent>();
            CreateMap<ExamSubjectComponent, ExamSubjectComponentResponse>();
        }
    }
}
