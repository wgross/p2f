using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.Paths;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface ICopyItemPropertySource
    {
        object CopyItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        IItemProvider GetItemProvider();
    }

    public interface ICopyItemPropertyDestination
    {
        void CopyItemProperty(IProviderContext providerContext, string sourcePropertyName, string destinationPropertyName, IItemProvider sourceItemProvider);
    }
}