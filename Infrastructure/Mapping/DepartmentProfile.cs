using Application.ViewModels.Branch;
using Application.ViewModels.Department;
using AutoMapper;
using Domain.Entities.Postgres.Scaffolded;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Mapping
{
    public class DepartmentProfile : Profile
    {
        public DepartmentProfile()
        {
            CreateMap<Department, DepartmentResponse>();

            CreateMap<DepartmentRequest, Department>();
        }
    }
}
