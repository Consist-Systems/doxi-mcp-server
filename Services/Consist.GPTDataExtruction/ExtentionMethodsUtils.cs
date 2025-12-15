using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Consist.GPTDataExtruction
{
    public static class ExtentionMethodsUtils
    {
        public static byte[] ToByteArray(this string source)
        {
            if (string.IsNullOrEmpty(source))
                return Array.Empty<byte>();

            return Encoding.UTF8.GetBytes(source);
        }
    }
}
