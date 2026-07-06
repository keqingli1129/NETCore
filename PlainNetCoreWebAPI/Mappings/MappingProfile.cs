using AutoMapper;
using PlainNetCoreWebAPI.Dtos;
using PlainNetCoreWebAPI.Models;

namespace PlainNetCoreWebAPI.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Category, CategoryDto>();
        // Reverse map for writes: members absent from the DTO (Picture, Products) are left untouched
        CreateMap<CategoryDto, Category>()
            .ForMember(d => d.Picture, opt => opt.Ignore())
            .ForMember(d => d.Products, opt => opt.Ignore());
    }
}
