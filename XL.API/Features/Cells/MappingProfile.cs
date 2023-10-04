using AutoMapper;
using XL.API.Data.Models;
using XL.API.Models;

namespace XL.API.Features.Cells;

public class CellMappingProfile : Profile
{
    public CellMappingProfile()
    {
        CreateMap<SheetCell, CellApiResponse>()
            .ForMember(x => x.Value, cfg => cfg.MapFrom(x => x.Expression))
            .ForMember(x => x.Result, cfg => cfg.MapFrom(x => x.NumericValue != null ? x.NumericValue.Value.ToString("G29") : x.Expression));
    }
}