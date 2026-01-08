using AutoMapper;
using TaskApp.Application.DTOs;
using TaskApp.Domain.Entities;

namespace TaskApp.Application.Mappings;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<User, UserResponse>();
        CreateMap<User, UserRefDto>();
        CreateMap<UserCreateRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.Ignore());

        // Task mappings are handled manually in services due to complex logic
    }
}

