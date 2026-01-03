using System.Threading.Tasks;

namespace Converge.Configuration.Application.Handlers
{
    /// <summary>
    /// Handle a specific request and return a result.
    /// Implementations encapsulate business logic for a single request type.
    /// </summary>
    public interface IRequestHandler<TRequest, TResult>
    {
        Task<TResult> Handle(TRequest request);
    }
}
