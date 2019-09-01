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
            CreateMap<Task, TaskDto>();
            CreateMap<TaskDto, Task>();
            CreateMap<TestCase, TestCaseDto>();
            CreateMap<TestCaseDto, TestCase>();
        }
    }
}
