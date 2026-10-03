using Application.ViewModels.Attendance;
using AutoMapper;

namespace Infrastructure.Mapping
{
    public class AttendanceProfile : Profile
    {
        public AttendanceProfile()
        {
            CreateMap<Attendance, AttendanceResponse>();
            CreateMap<AttendanceRequest, Attendance>();
        }
    }
}
