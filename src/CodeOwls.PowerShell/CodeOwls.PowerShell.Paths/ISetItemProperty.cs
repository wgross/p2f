using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface ISetItemProperty
    {
        object SetItemPropertyParameters { get; }

        void SetItemProperties(IProviderContext providerContext, IEnumerable<PSPropertyInfo> properties);
    }
}