using AutoMapper;
using Invaise.BusinessDomain.API.Entities;
using Invaise.BusinessDomain.API.Models;

namespace Invaise.BusinessDomain.API.Profiles;

/// <summary>
/// Profile for mapping data into and from User entities.
/// </summary>
public class UserProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserProfile"/> class.
    /// </summary>
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.DisplayName))
            .ForMember(dest => dest.PersonalInfo, opt => opt.MapFrom(src => src.PersonalInfo))
            .ForMember(dest => dest.Preferences, opt => opt.MapFrom(src => src.Preferences));
            
        CreateMap<UserPersonalInfo, UserPersonalInfoDto>()
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth));
        
        CreateMap<UserPersonalInfoModel, UserPersonalInfo>()
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.LegalFirstName, opt => opt.Ignore())
            .ForMember(dest => dest.LegalLastName, opt => opt.Ignore())
            .ForMember(dest => dest.City, opt => opt.Ignore())
            .ForMember(dest => dest.PostalCode, opt => opt.Ignore())
            .ForMember(dest => dest.Country, opt => opt.Ignore())
            .ForMember(dest => dest.GovernmentId, opt => opt.Ignore());
        
        CreateMap<UserPreferences, UserPreferencesDto>();

        CreateMap<ServiceAccount, UserDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.PersonalInfo, opt => opt.Ignore())
            .ForMember(dest => dest.Preferences, opt => opt.Ignore());
    }
} 