using Application.ViewModels.Notification;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile() {

            CreateMap<Notification, NotificationResponse>();
            CreateMap<NotificationRequest, Notification>();


        }
    }
}
