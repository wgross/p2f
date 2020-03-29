using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface IMoveItem
    {
        object MoveItemParameters { get; }

        void MoveItem(IProviderContext providerContext, string path, string movePath, PathNode destinationContainer);
    }
}