using Application.ViewModels.Staff;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class StaffProfile : Profile
    {
        public StaffProfile() 
        {
            CreateMap<Staff, StaffResponse>();
            CreateMap<StaffRequest, Staff>();
        }
    }
}
