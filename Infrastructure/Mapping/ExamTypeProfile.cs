using Application.ViewModels.ExamType;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ExamTypeProfile : Profile
    {
        public ExamTypeProfile()
        {
            CreateMap<ExamTypeRequest, ExamType>();
            CreateMap<ExamType, ExamTypeResponse>();

        }
    }
}
