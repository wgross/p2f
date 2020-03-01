using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetItemProperty
    {
        object GetItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        IEnumerable<PSPropertyInfo> GetItemProperties(IProviderContext providerContext, IEnumerable<string> propertyNames);
    }
}