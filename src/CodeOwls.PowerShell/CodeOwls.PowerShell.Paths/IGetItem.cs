using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IGetItem
    {
        string Name { get; }

        bool IsContainer { get; }

        object GetItemParameters => new RuntimeDefinedParameterDictionary();

        //object GetItem(IProviderContext providerContext, string path);

        object GetItem();
    }
}