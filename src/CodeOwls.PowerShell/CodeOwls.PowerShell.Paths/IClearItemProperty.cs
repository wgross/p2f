using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IClearItemProperty
    {
        object ClearItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        void ClearItemProperty(IProviderContext providerContext, IEnumerable<string> propertyToClear);
    }
}