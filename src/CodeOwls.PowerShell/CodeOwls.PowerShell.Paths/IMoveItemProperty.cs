using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using CodeOwls.PowerShell.Provider.PathNodes;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IMoveItemPropertySource : IRemoveItemProperty
    {
        object MoveItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        IItemProvider GetItemProvider();
    }

    public interface IMoveItemPropertyDestination
    {
        void MoveItemProperty(IProviderContext providerContext, string sourceProperty, string destinationProperty, IItemProvider sourceItemProvider);
    }
}