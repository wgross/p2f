using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface INewItemProperty
    {
        object NewItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        void NewItemProperty(IProviderContext providerContext, string propertyName, string propertyTypeName, object newItemValue);
    }
}