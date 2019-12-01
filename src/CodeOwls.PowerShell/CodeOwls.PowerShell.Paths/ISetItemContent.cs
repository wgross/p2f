using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace CodeOwls.PowerShell.Paths
{
    public interface ISetItemContent
    {
        IContentWriter GetContentWriter(IProviderContext providerContext);

        object GetContentWriterDynamicParameters(IProviderContext providerContext)
            => new RuntimeDefinedParameterDictionary();
    }
}