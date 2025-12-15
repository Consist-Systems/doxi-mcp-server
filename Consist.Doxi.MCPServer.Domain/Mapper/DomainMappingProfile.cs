using AutoMapper;
using Consist.GPTDataExtruction.Model;
using Consist.PDFTools.Model;

namespace Consist.Doxi.MCPServer.Domain.Mapper
{
    public class DomainMappingProfile : Profile
    {
        public DomainMappingProfile()
        {
            // Map from GPTDataExtruction TextElement to PDFTools TextElement
            CreateMap<Consist.GPTDataExtruction.Model.TextElement, Consist.PDFTools.Model.TextElement>();
        }
    }
}
