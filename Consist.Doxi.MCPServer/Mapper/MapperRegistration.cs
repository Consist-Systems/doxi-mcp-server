using AutoMapper;
using Consist.Doxi.Domain.Models;
using Consist.Doxi.MCPServer.Models;

namespace Consist.Doxi.MCPServer.Mapper
{
    public class MapperRegistration : Profile
    {
        public MapperRegistration()
        {
            CreateMap<AddTemplateRequest, ExAddTemplateRequest>();
        }
    }
}
