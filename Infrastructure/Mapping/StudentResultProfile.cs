using Application.ViewModels.StudentResult;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class StudentResultProfile : Profile
    {
        public StudentResultProfile() 
        {
            CreateMap<StudentResult, StudentResultResponse>();
            CreateMap<StudentResultRequest, StudentResult>();


        }
    }
}
