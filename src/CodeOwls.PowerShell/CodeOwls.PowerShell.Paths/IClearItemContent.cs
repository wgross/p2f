using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IClearItemContent
    {
        void ClearContent(IProviderContext providerContext);

        object ClearContentDynamicParameters(IProviderContext providerContext)
            => new RuntimeDefinedParameterDictionary();
    }
}