using Application.ViewModels.StaffRole;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class StaffRoleProfile : Profile
    {
        public StaffRoleProfile() 
        {
            CreateMap<StaffRole, StaffRoleResponse>();
            CreateMap<StaffRoleRequest, StaffRole>();

        }
    }
}
