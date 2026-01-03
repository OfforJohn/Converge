using System;
using System.Threading.Tasks;

namespace Converge.Configuration.Application.Handlers
{
    /// <summary>
    /// Simple dispatcher that resolves handler instances from IServiceProvider and invokes them.
    /// </summary>
    public class RequestDispatcher : IRequestDispatcher
    {
        private readonly IServiceProvider _provider;

        public RequestDispatcher(IServiceProvider provider)
        {
            _provider = provider;
        }

        public Task<TResult> Send<TRequest, TResult>(TRequest request) where TRequest : class
        {
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(typeof(TRequest), typeof(TResult));
            var handler = _provider.GetService(handlerType);
            if (handler == null)
                throw new InvalidOperationException($"Handler for {typeof(TRequest).FullName} not registered");

            return ((dynamic)handler).Handle((dynamic)request);
        }
    }
}
