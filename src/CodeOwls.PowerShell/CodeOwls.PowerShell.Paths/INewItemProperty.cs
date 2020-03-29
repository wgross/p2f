using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface INewItemProperty
    {
        object NewItemPropertyParameters { get; }

        void NewItemProperty(IProviderContext providerContext, string propertyName, string propertyTypeName, object newItemValue);
    }
}