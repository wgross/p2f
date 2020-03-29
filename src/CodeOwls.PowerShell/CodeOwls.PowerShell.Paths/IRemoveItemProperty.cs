using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface IRemoveItemProperty
    {
        object RemoveItemPropertyParameters { get; }

        void RemoveItemProperty(IProviderContext providerContext, string propertyName);
    }
}