using Application.ViewModels.Certificates;
using AutoMapper;
using Infrastructure;

namespace Infrastructure.Mapping;

public class HallTicketProfile : Profile
{
    public HallTicketProfile()
    {
        CreateMap<HallTicketRequest, HallTicket>();
        CreateMap<HallTicket, HallTicketResponse>();
    }
}
