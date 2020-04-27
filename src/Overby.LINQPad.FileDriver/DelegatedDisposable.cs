using System;

namespace Overby.LINQPad.FileDriver
{
    public class DelegatedDisposable : IDisposable
    {   
        private readonly Action _onDispose;

        public DelegatedDisposable(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }
}
