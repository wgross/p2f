using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetItemProperty
    {
        object GetItemPropertyParameters { get; }

        IEnumerable<PSPropertyInfo> GetItemProperties(IProviderContext providerContext, IEnumerable<string> propertyNames);
    }
}