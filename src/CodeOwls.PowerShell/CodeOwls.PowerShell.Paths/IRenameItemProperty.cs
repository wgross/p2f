using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Paths
{
    public interface IRenameItemProperty
    {
        object RenameItemPropertyParameters { get; }

        void RenameItemProperty(IProviderContext providerContext, string sourcePropertyNamee, string destinationPropertyName);
    }
}