using CodeOwls.PowerShell.Paths;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider : IPropertyCmdletProvider
    {
        #region GetProperty

        public void GetProperty(string path, Collection<string> providerSpecificPickList)
            => ExecuteAndLog(() => DoGetProperty(path, providerSpecificPickList), nameof(GetProperty), path, providerSpecificPickList.ToArgList());

        private void DoGetProperty(string path, Collection<string> providerSpecificPickList)
            => GetNodeFactoryFromPath(path).ToList().ForEach(f => GetProperty(path, f, providerSpecificPickList));

        private void GetProperty(string path, PathNode factory, Collection<string> providerSpecificPickList)
        {
            var pso = TryMakePsObjectFromPathNode(factory, providerSpecificPickList);
            if (pso.created)
            {
                WritePropertyObject(pso.psObject, path);
            }
        }

        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList) => null;

        #endregion GetProperty

        #region SetPropery

        public void SetProperty(string path, PSObject propertyValue)
            => ExecuteAndLog(() => DoSetProperty(path, propertyValue), nameof(SetProperty), path, propertyValue.ToArgString());

        private void DoSetProperty(string path, PSObject propertyValue)
            => GetNodeFactoryFromPath(path).ToList().ForEach(f => SetProperty(path, f, propertyValue));

        private void SetProperty(string path, PathNode factory, PSObject propertyValue)
            => factory.GetItemProvider().SetItemProperties(propertyValue.Properties);

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue) => null;

        #endregion SetPropery

        #region ClearProperty

        public void ClearProperty(string path, Collection<string> propertyToClear)
             => ExecuteAndLog(() => ClearPropertyImpl(path, propertyToClear), nameof(ClearProperty), path);

        private void ClearPropertyImpl(string path, Collection<string> propertyToClear)
            => GetNodeFactoryFromPath(path).OfType<IClearItemProperty>().ToList().ForEach(pathNode => ClearProperty(path, pathNode, propertyToClear));

        private void ClearProperty(string path, IClearItemProperty pathNodeCapability, Collection<string> propertyToClear)
        {
            pathNodeCapability.ClearItemProperty(CreateContext(path), propertyToClear);
        }

        public object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
            => GetPathNodesFromPath<IClearItemProperty>(path).FirstOrDefault()?.ClearItemPropertyParameters;

        #endregion ClearProperty
    }
}