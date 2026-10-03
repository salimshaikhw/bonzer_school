using Application.ViewModels.FeeHead;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class FeeHeadProfile : Profile
    {
        public FeeHeadProfile()
        {
            CreateMap<FeeHead, FeeHeadResponse>();
            CreateMap<FeeHeadRequest, FeeHead>();


        }
    }
}
