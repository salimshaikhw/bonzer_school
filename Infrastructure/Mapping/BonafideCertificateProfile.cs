using Application.ViewModels.Certificates;
using AutoMapper;
using Infrastructure;

namespace Infrastructure.Mapping;

public class BonafideCertificateProfile : Profile
{
    public BonafideCertificateProfile()
    {
        CreateMap<BonafideCertificateRequest, BonafideCertificate>();
        CreateMap<BonafideCertificate, BonafideCertificateResponse>();
    }
}
