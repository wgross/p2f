using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface IMoveItemPropertySource : IRemoveItemProperty
    {
        object MoveItemPropertyParameters { get; }

        object GetItemProvider();
    }

    public interface IMoveItemPropertyDestination
    {
        void MoveItemProperty(IProviderContext providerContext, string sourceProperty, string destinationProperty, PathNode sourceItemProvider);
    }
}