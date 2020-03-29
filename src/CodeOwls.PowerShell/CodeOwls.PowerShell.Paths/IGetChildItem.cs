using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetChildItem
    {
        object GetChildItemParameters { get; }

        IEnumerable<PathNode> GetChildNodes(IProviderContext providerContext);
    }
}