using AutoMapper;
using CMMS.Core.Entities;
using CMMS.Core.Models;

namespace CMMS.API.Helpers
{
    public class ApplicationMapper : Profile
    {
        public ApplicationMapper()
        {
            #region User
            CreateMap<ApplicationUser, UserDTO>().ReverseMap();
            CreateMap<ApplicationUser, UserRolesVM>().ReverseMap();
            CreateMap<ApplicationUser, UserVM>().ReverseMap();
            #endregion
          
        }
    }
}
