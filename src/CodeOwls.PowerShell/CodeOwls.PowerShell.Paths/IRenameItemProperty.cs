using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IRenameItemProperty
    {
        object RenameItemPropertyParameters => new RuntimeDefinedParameterDictionary();

        void RenameItemProperty(IProviderContext providerContext, string sourcePropertyNamee, string destinationPropertyName);
    }
}