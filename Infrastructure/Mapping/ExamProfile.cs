using Application.ViewModels.Exam;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ExamProfile : Profile
    {
        public ExamProfile() {
            CreateMap<Exam, ExamResponse>();
            CreateMap<ExamRequest, Exam>();
        }
    }
}
