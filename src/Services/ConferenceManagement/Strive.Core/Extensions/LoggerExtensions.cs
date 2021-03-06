using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace Strive.Core.Extensions
{
    public static class LoggerExtensions
    {
        public static IDisposable BeginMethodScope(this ILogger logger, object state,
            [CallerMemberName] string method = "Default")
        {
            return new CompositeDisposables(logger.BeginScope($"{method}()"), logger.BeginScope(state));
        }

        private class CompositeDisposables : IDisposable
        {
            private readonly IDisposable?[] _disposables;

            public CompositeDisposables(params IDisposable?[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                foreach (var disposable in _disposables) disposable?.Dispose();
            }
        }
    }
}
