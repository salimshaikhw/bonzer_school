using Application.ViewModels.ParentUser;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class ParentUserProfile : Profile
    {
        public ParentUserProfile() {
        CreateMap<ParentUser,ParentUserResponse>();
            CreateMap<ParentUserRequest, ParentUser>();


        }
    }
}
