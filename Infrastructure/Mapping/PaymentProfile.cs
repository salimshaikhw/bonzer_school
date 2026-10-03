using Application.ViewModels.Payment;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class PaymentProfile : Profile
    {
        public PaymentProfile() {

            CreateMap<Payment, PaymentResponse>();
            CreateMap<PaymentRequest, Payment>();

        }
    }
}
