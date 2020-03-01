using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface ISetItemProperty
    {
        object SetItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        void SetItemProperties(IProviderContext providerContext, IEnumerable<PSPropertyInfo> properties);
    }
}