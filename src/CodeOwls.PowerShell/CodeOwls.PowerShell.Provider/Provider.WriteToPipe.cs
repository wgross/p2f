using CodeOwls.PowerShell.Paths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        private static (bool created, PSObject psObject, bool isCollection) TryMakePsObjectFromPathNode(PathNode pathNode, IEnumerable<string> propertyNames)
        {
            var itemProvider = pathNode.GetItemProvider();
            if (itemProvider is null)
            {
                return (false, null, false);
            }

            if (propertyNames is null || !propertyNames.Any())
            {
                var item = itemProvider.GetItem();
                PSObject psObject = null;
                if (item is PSObject pso)
                {
                    psObject = pso;
                }
                else
                {
                    psObject = PSObject.AsPSObject(itemProvider.GetItem());
                }
                psObject.Properties.Add(new PSNoteProperty(ItemModePropertyName, pathNode.ItemMode));

                itemProvider
                    .GetItemProperties(propertyNames)
                    .Aggregate(psObject.Properties, (psoProps, p) =>
                    {
                        psoProps.Add(p);
                        return psoProps;
                    });
                return (true, psObject, itemProvider.IsContainer);
            }
            else
            {
                var psObject = new PSObject();

                itemProvider
                    .GetItemProperties(propertyNames)
                    .Aggregate(psObject.Properties, (psoProps, p) =>
                    {
                        psoProps.Add(p);
                        return psoProps;
                    });

                return (true, psObject, itemProvider.IsContainer);
            }
        }

        private void WriteGeneralCmdletError(Exception exception, string errorId, string path)
        {
            this.WriteError(new ErrorRecord(
                exception,
                errorId,
                ErrorCategory.NotSpecified,
                path
            ));
        }
    }
}