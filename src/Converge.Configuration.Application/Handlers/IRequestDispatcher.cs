using System.Threading.Tasks;

namespace Converge.Configuration.Application.Handlers
{
    /// <summary>
    /// Dispatches requests (commands/queries) to their respective handlers.
    /// </summary>
    public interface IRequestDispatcher
    {
        Task<TResult> Send<TRequest, TResult>(TRequest request) where TRequest : class;
    }
}
