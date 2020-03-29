using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetItem
    {
        object GetItemParameters { get; }

        PSObject GetItem(IProviderContext providerContext);
    }
}