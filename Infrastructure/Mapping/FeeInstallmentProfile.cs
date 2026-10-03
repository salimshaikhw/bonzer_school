using Application.ViewModels.FeeInstallment;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class FeeInstallmentProfile : Profile
    {
        public FeeInstallmentProfile()
        {
            CreateMap<FeeInstallment, FeeInstallmentResponse>();
            CreateMap<FeeInstallmentRequest, FeeInstallment>();

        }
    }
}
