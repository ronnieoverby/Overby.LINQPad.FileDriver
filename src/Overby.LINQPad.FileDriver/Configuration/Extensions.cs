using System.Collections.Generic;
using System.Linq;

namespace Overby.LINQPad.FileDriver.Configuration
{
    public static class Extensions
    {

        public static ValueTokens ValueTokens(this IEnumerable<string> tokens, bool ignoreCase) =>
                new ValueTokens(ignoreCase, tokens.ToArray());
    }
}
