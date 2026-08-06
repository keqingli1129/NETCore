using AutoMapper;
using NZWalks.API.Models;
using NZWalks.API.Models.DTOs;

namespace NZWalks.API.Mappings;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<Region, RegionDto>().ReverseMap();
        CreateMap<AddRegionRequestDto, Region>()
            .ForMember(dest => dest.RegionImageUrl, opt => opt.Ignore());
        CreateMap<UpdateRegionRequestDto, Region>()
            .ForMember(dest => dest.RegionImageUrl, opt => opt.Ignore());

        CreateMap<Walk, WalkDto>().ReverseMap();
        CreateMap<AddWalkRequestDto, Walk>();
        CreateMap<UpdateWalkRequestDto, Walk>();

        CreateMap<Difficulty, DifficultyDto>().ReverseMap();
        CreateMap<AddDifficultyRequestDto, Difficulty>();
        CreateMap<UpdateDifficultyRequestDto, Difficulty>();
    }
}
