using Application.ViewModels.ExamComponentType;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ExamComponentTypeProfile : Profile
    {
        public ExamComponentTypeProfile()
        {
            CreateMap<ExamComponentType, ExamComponentTypeResponse>();
            CreateMap<ExamComponentTypeRequest, ExamComponentType>();


        }
    }
}
