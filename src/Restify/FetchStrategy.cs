namespace Restify
{
    /// <summary>
    /// Determines whether to fetch data from the Api or the Local cache
    /// </summary>
    public enum FetchStrategy
    {
        /// <summary>
        /// If the network is connected, fetch from the Api first and fallback to the cache if Api fetch is not successful
        /// </summary>
        ApiThenCache,
        /// <summary>
        /// Do not attempt to fetch from the Api, go straight to the cache
        /// </summary>
        CacheOnly
    }
}