using Application.ViewModels.AcademicYear;
using AutoMapper;

namespace Infrastructure.Mapping
{
    public class AcademincYearProfile : Profile
    {
        public AcademincYearProfile()
        {
            CreateMap<AcademicYear, AcademicYearResponse>();
            CreateMap<AcademicYearRequest, AcademicYear>();


        }
    }
}
