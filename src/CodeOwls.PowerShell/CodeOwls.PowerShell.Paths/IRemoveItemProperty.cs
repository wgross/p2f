using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IRemoveItemProperty
    {
        object RemoveItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        void RemoveItemProperty(IProviderContext providerContext, string propertyName);
    }
}