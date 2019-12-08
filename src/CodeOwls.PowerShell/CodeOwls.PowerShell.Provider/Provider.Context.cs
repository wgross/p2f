using CodeOwls.PowerShell.Provider.PathNodeProcessors;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        protected virtual IProviderContext CreateContext(string path) => CreateContext(path, false, true);

        protected virtual IProviderContext CreateContext(string path, bool recurse) => CreateContext(path, recurse, false);

        protected virtual IProviderContext CreateContext(string path, bool recurse, bool resolveFinalNodeFilterItems)
        {
            return new ProviderContext(this, path, PSDriveInfo, PathResolver, DynamicParameters, recurse)
            {
                ResolveFinalNodeFilterItems = resolveFinalNodeFilterItems
            };
        }
    }
}