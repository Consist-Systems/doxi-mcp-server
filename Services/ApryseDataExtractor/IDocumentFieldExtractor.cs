using ApryseDataExtractor.Models;
using Consist.Doxi.Domain.Models;

namespace ApryseDataExtractor
{
    public interface IDocumentFieldExtractor
    {
        Task<IEnumerable<ExTemplatFlowElement>> GetDocumentElements(byte[] documentBytes, string languages);

        Task<DocumentStructure> GetDocumentStructure(byte[] documentBytes, string languages);

        Task<DocumentFieldsPosition> GetDocumentFields(byte[] documentBytes, string languages);
    }
}
