using Application.ViewModels.Role;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class RoleProfile : Profile
    {
        public RoleProfile()
        {
            CreateMap<RoleRequest, Role>();
            CreateMap<Role, RoleResponse>();
        }

    }
}
