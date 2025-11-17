using Consist.Doxi.Domain.Models;

namespace ApryseDataExtractor
{
    public interface IDocumentFieldExtractor
    {
        Task<IEnumerable<ExTemplatFlowElement>> GetDocumentElements(byte[] documentBytes);
    }
}
