using LINQPad;
using System.Collections.Generic;

namespace Overby.LINQPad.FileDriver
{
    public static class PublicExtensions
    {
        public static void WriteCsv<T>(this IEnumerable<T> sequence, string path)
        {
            Util.WriteCsv(sequence, path);
        }
    }
}
