using AutoMapper;
using OnlineJudgeApi.Entities;
using OnlineJudgeApi.Dtos;

namespace OnlineJudgeApi.Helpers
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<UserDto, User>();
        }
    }
}
