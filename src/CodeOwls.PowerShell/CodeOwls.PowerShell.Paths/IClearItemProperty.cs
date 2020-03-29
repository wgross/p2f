using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Collections.Generic;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IClearItemProperty
    {
        object ClearItemPropertyParameters { get; }

        void ClearItemProperty(IProviderContext providerContext, IEnumerable<string> propertyToClear);
    }
}