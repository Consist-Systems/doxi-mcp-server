using Consist.Doxi.Domain.Models;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.Doxi.MCPServer.Models
{
    /// <summary>
    /// Represents a request to add a new template.
    /// </summary>
    /// <remarks>This class is used to encapsulate the data required to create a new template. The specific
    /// properties and fields of this class should be populated with the necessary information before sending the
    /// request.</remarks>
    public class AddTemplateRequest : ExBaseTemplate
    {
        /// <summary>
        /// The file that the template is base on, need to pass PDF/Word/Image file
        /// </summary>
        public DataContent TemplateDocument { get; set; }
    }
}
