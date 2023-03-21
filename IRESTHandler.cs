
namespace LRSLocator
{
    /// <summary>
    /// Interface that handles incoming Rest requests
    /// </summary>
    interface IRESTHandler
    {
        /// <summary>
        /// Handles a request to a specific REST resource or operation.
        /// The return value can be JsonObject, string, or byte[].
        /// </summary>
        object HandleRequest(RESTContext context);
    }
}
