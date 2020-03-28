using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Paths.Exceptions;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider : IDynamicPropertyCmdletProvider
    {
        #region NewProperty

        public void _NewProperty(string path, string propertyName, string propertyTypeName, object value)
        {
            using (var call = this.StepInProviderMethod(path))
            {
                call
                    .Resolve<INewItemProperty>(path)
                    .InvokeWithContext((ctx, nip) => nip.Foreach(n => n.NewItemProperty(ctx, path, propertyName, value)));
            }
        }

        public void NewProperty(string path, string propertyName, string propertyTypeName, object value)
        {
            this.ExecuteAndLog(
                action: () => this.NewPropertyImpl(path, propertyName, propertyTypeName, value),
                methodName: nameof(NewProperty),
                path, propertyName, propertyTypeName);
        }

        private void NewPropertyImpl(string path, string propertyName, string propertyTypeName, object value)
        {
            var pathNodes = GetPathNodesFromPath<INewItemProperty>(path).ToList();
            if (pathNodes.Any())
            {
                pathNodes.ForEach(n => n.NewItemProperty(CreateContext(path), propertyName, propertyTypeName, value));
            }
            else
            {
                this.WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.NewItemProperty, NotImplementedErrorId);
            }
        }

        public object NewPropertyDynamicParameters(string path, string propertyName, string propertyTypeName, object value)
        {
            return this.ExecuteAndLog<object>(
               action: () => this.NewPropertyDynamicParametersImpl(path, propertyName, propertyTypeName, value),
               methodName: nameof(NewPropertyDynamicParameters),
               path, propertyName, propertyTypeName);
        }

        private object NewPropertyDynamicParametersImpl(string path, string propertyName, string propertyTypeName, object value)
            => GetPathNodesFromPath<INewItemProperty>(path).FirstOrDefault()?.NewItemPropertyParameters;

        #endregion NewProperty

        #region CopyProperty

        public void CopyProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
        {
            this.ExecuteAndLog(
               action: () => this.CopyPropertyImpl(sourcePath, sourceProperty, destinationPath, destinationProperty),
               methodName: nameof(CopyProperty),
               sourcePath, sourceProperty, destinationPath, destinationProperty);
        }

        public void CopyPropertyImpl(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
        {
            var sourcePathNode = GetNodeFactoryFromPath(sourcePath).FirstOrDefault();
            if (!sourcePathNode.TryGetCapability<ICopyItemPropertySource>().has)
            {
                this.WriteCmdletNotSupportedAtNodeError(sourcePath, ProviderCmdlet.CopyItemProperty, NotImplementedErrorId);
                return;
            }

            var destinationPathNode = GetNodeFactoryFromPathOrParent(destinationPath, out var isParentNode).FirstOrDefault();
            if (destinationPathNode is null)
            {
                WriteError(new ErrorRecord(
                    new CopyOrMoveToNonexistentContainerException(destinationPath),
                    CopyItemDestinationContainerDoesNotExistErrorID,
                    ErrorCategory.WriteError,
                    destinationPath
                ));
            }

            var copyDestinationPathNode = destinationPathNode as ICopyItemPropertyDestination;
            if (destinationPathNode is null)
            {
                this.WriteCmdletNotSupportedAtNodeError(destinationPath, ProviderCmdlet.CopyItemProperty, NotImplementedErrorId);
                return;
            }

            var sourceItemName = GetChildName(sourcePath);
            var destinationItemName = isParentNode ? GetChildName(destinationPath) : null;
            var destinationContainerPath = isParentNode ? GetParentPath(destinationPath, GetRootPath()) : destinationPath;

            copyDestinationPathNode.CopyItemProperty(CreateContext(destinationPath), sourceProperty, destinationProperty, sourcePathNode);
        }

        public object CopyPropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
             => GetPathNodesFromPath<ICopyItemPropertySource>(sourcePath).FirstOrDefault()?.CopyItemPropertyParameters;

        #endregion CopyProperty

        #region MoveItemProperty

        public void MoveProperty(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
        {
            this.ExecuteAndLog(
              action: () => this.MovePropertyImpl(sourcePath, sourceProperty, destinationPath, destinationProperty),
              methodName: nameof(CopyProperty),
              sourcePath, sourceProperty, destinationPath, destinationProperty);
        }

        public void MovePropertyImpl(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
        {
            var sourcePathNode = GetNodeFactoryFromPath(sourcePath).FirstOrDefault();
            if (!sourcePathNode.TryGetCapability<IMoveItemPropertySource>().has)
            {
                this.WriteCmdletNotSupportedAtNodeError(sourcePath, ProviderCmdlet.MoveItemProperty, NotImplementedErrorId);
                return;
            }

            if (!sourcePathNode.TryGetCapability<IRemoveItemProperty>().has)
            {
                this.WriteCmdletNotSupportedAtNodeError(sourcePath, ProviderCmdlet.RemoveItemProperty, NotImplementedErrorId);
                return;
            }

            var destinationPathNode = GetNodeFactoryFromPathOrParent(destinationPath, out var isParentNode).FirstOrDefault();
            if (destinationPathNode is null)
            {
                WriteError(new ErrorRecord(
                    new CopyOrMoveToNonexistentContainerException(destinationPath),
                    CopyItemDestinationContainerDoesNotExistErrorID,
                    ErrorCategory.WriteError,
                    destinationPath
                ));
            }

            var moveDestinationPathNode = destinationPathNode as IMoveItemPropertyDestination;
            if (destinationPathNode is null)
            {
                this.WriteCmdletNotSupportedAtNodeError(destinationPath, ProviderCmdlet.CopyItemProperty, NotImplementedErrorId);
                return;
            }

            var sourceItemName = GetChildName(sourcePath);
            var destinationItemName = isParentNode ? GetChildName(destinationPath) : null;
            var destinationContainerPath = isParentNode ? GetParentPath(destinationPath, GetRootPath()) : destinationPath;

            moveDestinationPathNode.MoveItemProperty(CreateContext(destinationPath), sourceProperty, destinationProperty, sourcePathNode);
            sourcePathNode.TryGetCapability<IRemoveItemProperty>().capability.RemoveItemProperty(CreateContext(destinationPath), sourceProperty);
        }

        public object MovePropertyDynamicParameters(string sourcePath, string sourceProperty, string destinationPath, string destinationProperty)
            => GetPathNodesFromPath<IMoveItemPropertySource>(sourcePath).FirstOrDefault()?.MoveItemPropertyParameters;

        #endregion MoveItemProperty

        #region RemoveItemProperty

        public void RemoveProperty(string path, string propertyName)
        {
            this.ExecuteAndLog(
                action: () => this.RemovePropertyImpl(path, propertyName),
                methodName: nameof(RemoveProperty),
                path, propertyName);
        }

        private void RemovePropertyImpl(string path, string propertyName)
        {
            var pathNodes = GetPathNodesFromPath<IRemoveItemProperty>(path).ToList();
            if (pathNodes.Any())
            {
                pathNodes.ForEach(n => n.RemoveItemProperty(CreateContext(path), propertyName));
            }
            else
            {
                this.WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.NewItemProperty, NotImplementedErrorId);
            }
        }

        public object RemovePropertyDynamicParameters(string path, string propertyName)
            => GetPathNodesFromPath<IRemoveItemProperty>(path).FirstOrDefault()?.RemoveItemPropertyParameters;

        #endregion RemoveItemProperty

        #region RenameProperty

        public void RenameProperty(string path, string sourceProperty, string destinationProperty)
        {
            this.ExecuteAndLog(
                action: () => this.RenamePropertyImpl(path, sourceProperty, destinationProperty),
                methodName: nameof(RenameProperty),
                path, sourceProperty, destinationProperty);
        }

        private void RenamePropertyImpl(string path, string sourceProperty, string destinationProperty)
        {
            var pathNodes = GetPathNodesFromPath<IRenameItemProperty>(path).ToList();
            if (pathNodes.Any())
            {
                pathNodes.ForEach(n => n.RenameItemProperty(CreateContext(path), sourceProperty, destinationProperty));
            }
            else
            {
                this.WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.NewItemProperty, NotImplementedErrorId);
            }
        }

        public object RenamePropertyDynamicParameters(string path, string sourceProperty, string destinationProperty)
        {
            return this.ExecuteAndLog<object>(
               action: () => this.RenamePropertyDynamicParametersImpl(path, sourceProperty, destinationProperty),
               methodName: nameof(RenamePropertyDynamicParameters),
               path, sourceProperty, destinationProperty); ;
        }

        private object RenamePropertyDynamicParametersImpl(string path, string sourceProperty, string destinationProperty)
            => GetPathNodesFromPath<IRenameItemProperty>(path).FirstOrDefault()?.RenameItemPropertyParameters;

        #endregion RenameProperty
    }
}