using Application.ViewModels.Invoice;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class InvoiceProfile : Profile
    {
        public InvoiceProfile() 
        {
            CreateMap<Invoice, InvoiceResponse>();
            CreateMap<InvoiceRequest, Invoice>();
        }
    }
}
