using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetItem
    {
        object GetItemParameters => new RuntimeDefinedParameterDictionary();

        PSObject GetItem(IProviderContext providerContext);
    }
}