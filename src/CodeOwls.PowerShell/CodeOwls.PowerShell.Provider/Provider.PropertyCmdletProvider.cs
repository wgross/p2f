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

        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
            => GetPathNodesFromPath<IGetItemProperty>(path).FirstOrDefault()?.GetItemPropertyParameters;

        #endregion GetProperty

        #region SetPropery

        public void SetProperty(string path, PSObject propertyValue)
            => ExecuteAndLog(() => DoSetProperty(path, propertyValue), nameof(SetProperty), path, propertyValue.ToArgString());

        private void DoSetProperty(string path, PSObject propertyValue)
            => GetPathNodesFromPath<ISetItemProperty>(path).ToList().ForEach(n => n.SetItemProperties(CreateContext(path), propertyValue.Properties));

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
            => ExecuteAndLog(() => DoSetPropertyDynamicParameters(path, propertyValue), nameof(SetPropertyDynamicParameters), path, propertyValue.ToArgString());

        private object DoSetPropertyDynamicParameters(string path, PSObject propertyValue)
            => GetPathNodesFromPath<ISetItemProperty>(path).FirstOrDefault()?.SetItemPropertyParameters;

        #endregion SetPropery

        #region ClearProperty

        public void ClearProperty(string path, Collection<string> propertyToClear)
             => ExecuteAndLog(() => ClearPropertyImpl(path, propertyToClear), nameof(ClearProperty), path);

        private void ClearPropertyImpl(string path, Collection<string> propertiesToClear)
            => GetPathNodesFromPath<IClearItemProperty>(path).ToList().ForEach(n => n.ClearItemProperty(CreateContext(path), propertiesToClear));

        private void ClearProperty(string path, IClearItemProperty pathNodeCapability, Collection<string> propertyToClear)
        {
            pathNodeCapability.ClearItemProperty(CreateContext(path), propertyToClear);
        }

        public object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
            => GetPathNodesFromPath<IClearItemProperty>(path).FirstOrDefault()?.ClearItemPropertyParameters;

        #endregion ClearProperty
    }
}