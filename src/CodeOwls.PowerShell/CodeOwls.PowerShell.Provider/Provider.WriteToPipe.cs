using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        #region Write a PathNode to the PowerShell default output as PSObject

        // Called from New-Item
        // move doesn outout in PS7, Set-Item most probably doesn't too.
        private void WritePathNode(IProviderContext providerContext, string nodePath, PathNode pathNode)
        {
            WriteItemObject(pathNode.GetItem(providerContext), MakePath(nodePath, pathNode.Name), pathNode.IsContainer);
        }

        private void WritePathNodeAsPSObject(IProviderContext providerContext, string nodePath, PathNode pathNode)
        {
            var pso = TryMakePsObjectFromPathNode(providerContext, nodePath, pathNode, propertyNames: Enumerable.Empty<string>());

            //nodeContainerPath = SessionState.Path.GetUnresolvedProviderPathFromPSPath(nodeContainerPath);

            if (pso.created)
            {
                WriteItemObject(pso.psObject, nodePath, pso.isCollection);
            }
        }

        private (bool created, PSObject psObject, bool isCollection) TryMakePsObjectFromPathNode(IProviderContext providerContext, string nodePath, PathNode pathNode, IEnumerable<string> propertyNames)
        {
            if (propertyNames is null || !propertyNames.Any())
            {
                return TryMakePsObjectFromAllProperties(providerContext, pathNode);
            }
            else
            {
                return TryMakePsObjectFromPathNodeWithPickList(nodePath, pathNode, propertyNames);
            }
        }

        private (bool created, PSObject psObject, bool isCollection) TryMakePsObjectFromAllProperties(IProviderContext providerContext, PathNode pathNode) => (true, pathNode.GetItem(providerContext), pathNode.IsContainer);

        private (bool created, PSObject psObject, bool isCollection) TryMakePsObjectFromPathNodeWithPickList(string nodePath, PathNode pathNode, IEnumerable<string> propertyNames)
        {
            var (has, getItemProperties) = pathNode.TryGetCapability<IGetItemProperty>();
            if (!has)
                throw new NotImplementedException($"{nameof(IGetItemProperty)} isn't implemented by {pathNode.Name} at {nodePath}");

            var psObject = getItemProperties
                .GetItemProperties(this.CreateContext(nodePath), propertyNames)
                .Aggregate(new PSObject(), (pso, p) =>
                {
                    // dynamic properties must NOT be refelection based.
                    // convert the C# properties to not properties
                    pso.Properties.Add(new PSNoteProperty(p.Name, p.Value));
                    return pso;
                });

            // if no propertiees are extracted, dont give anything back.
            return psObject.Properties.Any()
                ? (true, psObject, pathNode.IsContainer)
                : (false, null, pathNode.IsContainer);
        }

        #endregion Write a PathNode to the PowerShell default output as PSObject

        #region Write an Cmdlet Error to the error stream

        private void WriteGeneralCmdletError(Exception exception, string errorId, string path)
        {
            this.WriteError(new ErrorRecord(
                exception,
                errorId,
                ErrorCategory.NotSpecified,
                path
            ));
        }

        #endregion Write an Cmdlet Error to the error stream
    }
}