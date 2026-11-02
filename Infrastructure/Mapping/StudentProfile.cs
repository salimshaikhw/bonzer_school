using Application.ViewModels.Student;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class StudentProfile : Profile
    {
        public StudentProfile()
        {
            // DTOs keep the original API field name "PreviousExamBord" so existing clients keep working.
            CreateMap<StudentRequest, Student>()
                .ForMember(d => d.PreviousExamBoard, o => o.MapFrom(s => s.PreviousExamBord));

            CreateMap<Student, StudentResponse>()
                .ForMember(d => d.PreviousExamBord, o => o.MapFrom(s => s.PreviousExamBoard));
        }
    }

}
