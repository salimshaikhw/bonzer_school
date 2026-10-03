using Application.ViewModels.Mark;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class MarkProfile : Profile
    {
        public MarkProfile() {

            CreateMap<Mark, MarkResponse>();
            CreateMap<MarkRequest, Mark>();


        }
    }
}
