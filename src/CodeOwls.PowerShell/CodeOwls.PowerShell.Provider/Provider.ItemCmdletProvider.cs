using CodeOwls.PowerShell.Paths;
using System;
using System.Linq;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        #region GetItem

        protected override void GetItem(string path) => ExecuteAndLog(() => DoGetItem(path), nameof(GetItem), path);

        private void DoGetItem(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (!factories.Any())
            {
                return;
            }

            factories.ToList().ForEach(f => GetItem(path, f));
        }

        private void GetItem(string path, PathNode factory)
        {
            try
            {
                WritePathNodeAsPSObject(CreateContext(path), path, factory);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, GetItemInvokeErrorID, path);
            }
        }

        #endregion GetItem
    }
}