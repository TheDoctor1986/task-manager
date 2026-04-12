namespace TaskManagerApi.Mapping
{
    using AutoMapper;
    using TaskManagerApi.Dtos;
    using TaskManagerApi.Models;

    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TaskItem, TaskDto>();
            CreateMap<CreateTaskDto, TaskItem>();
            CreateMap<UpdateTaskDto, TaskItem>();
        }
    }
}
