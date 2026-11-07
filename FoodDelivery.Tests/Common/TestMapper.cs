using AutoMapper;
using FoodDelivery.Application;

namespace FoodDelivery.Tests.Common;

public static class TestMapper
{
    public static IMapper Instance { get; } =
        new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
}
