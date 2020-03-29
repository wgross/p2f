using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface ICopyItemPropertySource
    {
        object CopyItemPropertyParameters { get; }

        object GetItemProvider();
    }

    public interface ICopyItemPropertyDestination
    {
        void CopyItemProperty(IProviderContext providerContext, string sourcePropertyName, string destinationPropertyName, PathNode sourceNode);
    }
}