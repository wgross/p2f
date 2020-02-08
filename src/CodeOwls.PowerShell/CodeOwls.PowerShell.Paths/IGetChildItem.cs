using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetChildItem
    {
        public object GetChildItemParameters => new RuntimeDefinedParameterDictionary();

        IEnumerable<PathNode> GetChildNodes(IProviderContext providerContext);
    }
}