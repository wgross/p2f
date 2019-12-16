using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Paths.Processors;

namespace ProviderFramework_1_TheNullProvider
{
    /// <summary>
    /// the path node processor
    /// </summary>
    internal class NullPathResolver : PathResolverBase
    {
        /// <summary>
        /// returns the first node factory object in the path graph
        /// </summary>
        protected override PathNode Root
        {
            get
            {
                return new NullRootPathNode();
            }
        }
    }
}