using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Overby.LINQPad.FileDriver
{
    public class IdentifierComparer : IEqualityComparer<string>
    {
        readonly string _scope = Guid.NewGuid().ToString();

        public bool Equals( string x,  string y)
        {
            if (x == null)
                return y == null;

            var xid = x.ToIdentifier(_scope);
            var yid = x.ToIdentifier(_scope);

            return xid == yid;
        }

        public int GetHashCode(string obj)
        {
            return EqualityComparer<string>.Default.GetHashCode(obj?.ToIdentifier(_scope));
        }
    }

  
}
