using Application.ViewModels.Certificates;
using AutoMapper;
using Infrastructure;

namespace Infrastructure.Mapping;

public class TcCertificateProfile : Profile
{
    public TcCertificateProfile()
    {
        CreateMap<TcCertificateRequest, TcCertificate>();
        CreateMap<TcCertificate, TcCertificateResponse>();
    }
}
