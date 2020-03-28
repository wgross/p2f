﻿/*
	Copyright (c) 2014 Code Owls LLC

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to
	deal in the Software without restriction, including without limitation the
	rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
	sell copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
	IN THE SOFTWARE.
*/

using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Paths.Exceptions;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider.Attributes;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.XPath;

namespace CodeOwls.PowerShell.Provider
{
    //[CmdletProvider("YourProviderName", ProviderCapabilities.ShouldProcess)]

    public abstract partial class Provider : NavigationCmdletProvider,
        ICmdletProviderSupportsHelp,
        IContentCmdletProvider
    {
        internal Drive DefaultDrive
        {
            get
            {
                var drive = PSDriveInfo as Drive;

                if (null == drive)
                {
                    drive = ProviderInfo.Drives.FirstOrDefault() as Drive;
                }

                return drive;
            }
        }

        internal Drive GetDriveForPath(string path)
        {
            var name = Drive.GetDriveName(path);
            return (from drive in ProviderInfo.Drives
                    where StringComparer.InvariantCultureIgnoreCase.Equals(drive.Name, name)
                    select drive).FirstOrDefault() as Drive;
        }

        protected abstract IPathResolver PathResolver { get; }

        private IEnumerable<PathNode> ResolvePath(string path)
        {
            path = EnsurePathIsRooted(path);
            return PathResolver.ResolvePath(CreateContext(path), path);
        }

        private string NormalizeWhacks(string path)
        {
            if (PSDriveInfo is { } && !string.IsNullOrEmpty(PSDriveInfo.Root) && path.StartsWith(PSDriveInfo.Root))
            {
                return PSDriveInfo.Root + NormalizeWhacks(path.Substring(PSDriveInfo.Root.Length));
            }

            return path.Replace("/", "\\").TrimEnd('\\');
        }

        private string EnsurePathIsRooted(string path)
        {
            path = NormalizeWhacks(path);
            if (this.PSDriveInfo is { } && !string.IsNullOrEmpty(PSDriveInfo.Root))
            {
                var separator = PSDriveInfo.Root.EndsWith("\\") ? string.Empty : "\\";
                if (!path.StartsWith(PSDriveInfo.Root))
                {
                    path = PSDriveInfo.Root + separator + path;
                }
            }

            return path;
        }

        #region Implementation of ICmdletProviderSupportsHelp

        public string GetHelpMaml(string helpItemName, string path)
        {
            Func<string> a = () => DoGetHelpMaml(helpItemName, path);
            return ExecuteAndLog(a, "GetHelpMaml", helpItemName, path);
        }

        private string DoGetHelpMaml(string helpItemName, string path)
        {
            if (String.IsNullOrEmpty(helpItemName) || String.IsNullOrEmpty(path))
            {
                return String.Empty;
            }

            var parts = helpItemName.Split(new[] { '-' });
            if (2 != parts.Length)
            {
                return String.Empty;
            }

            var nodeFactory = GetFirstNodeFactoryFromPath(path);
            if (null == nodeFactory)
            {
                return String.Empty;
            }

            XmlDocument document = new XmlDocument();

            string filename = GetExistingHelpDocumentFilename();

            if (String.IsNullOrEmpty(filename)
                || !File.Exists(filename))
            {
                return String.Empty;
            }

            try
            {
                document.Load(filename);
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(
                               new MamlHelpDocumentExistsButCannotBeLoadedException(filename, e),
                               GetHelpCustomMamlErrorID,
                               ErrorCategory.ParserError,
                               filename
                               ));

                return string.Empty;
            }

            List<string> keys = GetCmdletHelpKeysForNodeFactory(nodeFactory);

            string verb = parts[0];
            string noun = parts[1];
            string maml = (from key in keys
                           let m = GetHelpMaml(document, key, verb, noun)
                           where !String.IsNullOrEmpty(m)
                           select m).FirstOrDefault();

            if (String.IsNullOrEmpty(maml))
            {
                maml = GetHelpMaml(document, NotSupportedCmdletHelpID, verb, noun);
            }
            return maml ?? String.Empty;
        }

        private List<string> GetCmdletHelpKeysForNodeFactory(PathNode pathNode)
        {
            var nodeFactoryType = pathNode.GetType();
            var idsFromAttributes =
                from CmdletHelpPathIDAttribute attr in
                    nodeFactoryType.GetCustomAttributes(typeof(CmdletHelpPathIDAttribute), true)
                select attr.ID;

            List<string> keys = new List<string>(idsFromAttributes);
            keys.AddRange(new[]
                              {
                                  nodeFactoryType.FullName,
                                  nodeFactoryType.Name,
                                  nodeFactoryType.Name.Replace("NodeFactory", ""),
                              });
            return keys;
        }

        private string GetExistingHelpDocumentFilename()
        {
            CultureInfo currentUICulture = Host.CurrentUICulture;
            string moduleLocation = this.ProviderInfo.Module.ModuleBase;
            string filename = null;
            while (currentUICulture != null && currentUICulture != currentUICulture.Parent)
            {
                string helpFilePath = GetHelpPathForCultureUI(currentUICulture.Name, moduleLocation);

                if (File.Exists(helpFilePath))
                {
                    filename = helpFilePath;
                    break;
                }
                currentUICulture = currentUICulture.Parent;
            }

            if (String.IsNullOrEmpty(filename))
            {
                string helpFilePath = GetHelpPathForCultureUI("en-US", moduleLocation);

                if (File.Exists(helpFilePath))
                {
                    filename = helpFilePath;
                }
            }

            LogDebug("Existing help document filename: {0}", filename);
            return filename;
        }

        private string GetHelpPathForCultureUI(string cultureName, string moduleLocation)
        {
            string documentationDirectory = Path.Combine(moduleLocation, cultureName);
            var path = Path.Combine(documentationDirectory, ProviderInfo.HelpFile);

            return path;
        }

        private string GetHelpMaml(XmlDocument document, string key, string verb, string noun)
        {
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(document.NameTable);
            nsmgr.AddNamespace("cmd", "http://schemas.microsoft.com/maml/dev/command/2004/10");

            string xpath = String.Format(
                "/helpItems/providerHelp/CmdletHelpPaths/CmdletHelpPath[@ID='{0}']/cmd:command[ ./cmd:details[ (cmd:verb='{1}') and (cmd:noun='{2}') ] ]",
                key,
                verb,
                noun);

            XmlNode node = null;
            try
            {
                node = document.SelectSingleNode(xpath, nsmgr);
            }
            catch (XPathException)
            {
                return string.Empty;
            }

            if (node == null)
            {
                return String.Empty;
            }

            return node.OuterXml;
        }

        #endregion Implementation of ICmdletProviderSupportsHelp

        #region NavigationCmdletProvider: IsItemContainer

        protected override bool IsItemContainer(string path)
        {
            return ExecuteAndLog(() => DoIsItemContainer(path), nameof(IsItemContainer), path);
        }

        private bool DoIsItemContainer(string path)
        {
            if (IsRootPath(path))
            {
                return true;
            }

            var node = GetFirstNodeFactoryFromPath(path);
            if (null == node)
            {
                return false;
            }

            return node.IsContainer;
        }

        #endregion NavigationCmdletProvider: IsItemContainer

        protected override object MoveItemDynamicParameters(string path, string destination)

        {
            return null;
        }

        protected override void MoveItem(string path, string destination)
        {
            Action a = () => DoMoveItem(path, destination);
            ExecuteAndLog(a, "MoveItem", path, destination);
        }

        private void DoMoveItem(string path, string destination)
        {
            var sourceNodes = GetNodeFactoryFromPath(path);
            sourceNodes.ToList().ForEach(n => MoveItem(path, n, destination));
        }

        private void MoveItem(string path, PathNode sourcePathNode, string destination)
        {
            var copyItem = sourcePathNode as ICopyItem;
            var moveItem = sourcePathNode as IMoveItem;
            var removeItem = copyItem as IRemoveItem;
            if (null == moveItem && (null == copyItem || null == removeItem))
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.MoveItem, MoveItemNotSupportedErrorID);
                return;
            }

            if (!ShouldProcess(path, ProviderCmdlet.MoveItem))
            {
                return;
            }

            try
            {
                if (null != moveItem)
                {
                    DoMoveItem(path, destination, moveItem);
                }
                else
                {
                    DoCopyItem(path, destination, true, copyItem);
                    DoRemoveItem(path, true, removeItem);
                }
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, MoveItemInvokeErrorID, path);
            }
        }

        private void DoMoveItem(string path, string destinationPath, IMoveItem moveItem)
        {
            var targetNode = GetNodeFactoryFromPathOrParent(destinationPath, out var targetNodeIsParentNode).FirstOrDefault();
            var sourceName = GetChildName(path);
            var finalDestinationPath = targetNodeIsParentNode ? GetChildName(destinationPath) : null;

            if (null == targetNode)
            {
                WriteError(new ErrorRecord(new CopyOrMoveToNonexistentContainerException(destinationPath),
                    CopyItemDestinationContainerDoesNotExistErrorID, ErrorCategory.WriteError, destinationPath));
                return;
            }

            moveItem.MoveItem(CreateContext(path), sourceName, finalDestinationPath, targetNode);
        }

        #region NavigationCmdletProvider: MakePath

        protected override string MakePath(string parent, string child)
            => ExecuteAndLog(() => DoMakePath(parent, child), nameof(MakePath), parent, child);

        private string DoMakePath(string parent, string child)
            => NormalizeWhacks(base.MakePath(parent, child));

        #endregion NavigationCmdletProvider: MakePath

        protected override string GetParentPath(string path, string root)
        {
            Func<string> a = () => DoGetParentPath(path, root);
            return ExecuteAndLog(a, "GetParentPath", path, root);
        }

        private string DoGetParentPath(string path, string root)
        {
            if (!path.Any())
            {
                return path;
            }

            path = NormalizeWhacks(base.GetParentPath(path, root));
            return path;
        }

        protected override string NormalizeRelativePath(string path, string basePath)
            => ExecuteAndLog(() => NormalizeWhacks(base.NormalizeRelativePath(path, basePath)), nameof(NormalizeRelativePath), path, basePath);

        protected override string GetChildName(string path) => ExecuteAndLog(() => DoGetChildName(path), nameof(GetChildName), path);

        private string DoGetChildName(string path)
        {
            var lastItem = NormalizeWhacks(path).Split('\\').Last();
            if (string.IsNullOrEmpty(lastItem))
                return this.PSDriveInfo.Root;
            return lastItem;
        }

        private void SetItem(string path, PathNode factory, object value)

        {
            var setItem = factory as ISetItem;
            if (null == factory || null == setItem)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.SetItem, SetItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;
            path = GetChildName(path);

            if (!ShouldProcess(fullPath, ProviderCmdlet.SetItem))
            {
                return;
            }

            try
            {
                setItem.SetItem(CreateContext(fullPath), path, value);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, SetItemInvokeErrorID, fullPath);
            }
        }

        protected override void SetItem(string path, object value)
        {
            Action a = () => DoSetItem(path, value);
            ExecuteAndLog(a, "SetItem", path, value.ToArgString());
        }

        private void DoSetItem(string path, object value)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (!factories.Any())
            {
                WriteError(
                    new ErrorRecord(
                        new ItemNotFoundException(path),
                        SetItemTargetDoesNotExistErrorID,
                        ErrorCategory.ObjectNotFound,
                        path
                        )
                    );
                return;
            }

            factories.ToList().ForEach(f => SetItem(path, f, value));
        }

        protected override object SetItemDynamicParameters(string path, object value)
        {
            Func<object> a = () => DoSetItemDynamicParameters(path);
            return ExecuteAndLog(a, "SetItemDynamicParameters", path, value.ToArgString());
        }

        private object DoSetItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var @set = factory as ISetItem;
            if (null == factory || null == @set)
            {
                return null;
            }

            return @set.SetItemParameters;
        }

        protected override object ClearItemDynamicParameters(string path)
        {
            Func<object> a = () => DoClearItemDynamicParameters(path);
            return ExecuteAndLog(a, "ClearItemDynamicParameters", path);
        }

        private object DoClearItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var clear = factory as IClearItem;
            if (null == factory || null == clear)
            {
                return null;
            }

            return clear.ClearItemDynamicParamters;
        }

        protected override void ClearItem(string path)
        {
            Action a = () => DoClearItem(path);
            ExecuteAndLog(a, "ClearItem", path);
        }

        private void DoClearItem(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (!factories.Any())
            {
                return;
            }

            factories.ToList().ForEach(f => ClearItem(path, f));
        }

        private void ClearItem(string path, PathNode factory)
        {
            var clear = factory as IClearItem;
            if (null == factory || null == clear)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.ClearItem, ClearItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;
            path = GetChildName(path);

            if (!ShouldProcess(fullPath, ProviderCmdlet.ClearItem))
            {
                return;
            }

            try
            {
                clear.ClearItem(CreateContext(fullPath), path);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, ClearItemInvokeErrorID, fullPath);
            }
        }

        private bool ForceOrShouldContinue(string itemName, string fullPath, string op)
        {
            if (Force || !ShouldContinue(ShouldContinuePrompt, String.Format("{2} {0} ({1})", itemName, fullPath, op)))
            {
                return false;
            }
            return true;
        }

        protected override object InvokeDefaultActionDynamicParameters(string path)
        {
            Func<object> a = () => DoInvokeDefaultActionDynamicParameters(path);
            return ExecuteAndLog(a, "InvokeDefaultActionDynamicParameters", path);
        }

        private object DoInvokeDefaultActionDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var invoke = factory as IInvokeItem;
            if (null == factory || null == invoke)
            {
                return null;
            }

            return invoke.InvokeItemParameters;
        }

        protected override void InvokeDefaultAction(string path)
        {
            Action a = () => DoInvokeDefaultAction(path);
            ExecuteAndLog(a, "InvokeDefaultAction", path);
        }

        private void DoInvokeDefaultAction(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (!factories.Any())
            {
                return;
            }

            factories.ToList().ForEach(f => InvokeDefaultAction(path, f));
        }

        private void InvokeDefaultAction(string path, PathNode factory)
        {
            var invoke = factory as IInvokeItem;
            if (null == factory || null == invoke)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.InvokeItem, InvokeItemNotSupportedErrorID);
                return;
            }

            var fullPath = path;

            if (!ShouldProcess(fullPath, ProviderCmdlet.InvokeItem))
            {
                return;
            }

            path = GetChildName(path);
            try
            {
                var results = invoke.InvokeItem(CreateContext(fullPath), path);
                if (null == results)
                {
                    return;
                }

                // TODO: determine what exactly to return here
                //  http://www.google.com/url?sa=t&rct=j&q=&esrc=s&source=web&cd=1&ved=0CCAQFjAA&url=http%3A%2F%2Fmsdn.microsoft.com%2Fen-us%2Flibrary%2Fsystem.management.automation.provider.itemcmdletprovider.invokedefaultaction(v%3Dvs.85).aspx&ei=28vLTpyrJ42utwfUo6WYAQ&usg=AFQjCNFto_ye_BBjxxWfzBFGfNxw3eEgTw
                //  docs tell me to return the item being invoked... but I'm not sure.
                //  is there any way for the target of the invoke to return data to the runspace??
                //results.ToList().ForEach(r => this.WriteObject(r));
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, InvokeItemInvokeErrorID, fullPath);
            }
        }

        #region ItemCmdletProvider: ItemExists

        protected override bool ItemExists(string path)
        {
            return ExecuteAndLog(() => DoItemExists(path), nameof(ItemExists), path);
        }

        private bool DoItemExists(string path)
        {
            if (IsRootPath(path))
            {
                return true;
            }

            return GetNodeFactoryFromPath(path).Any();
        }

        #endregion ItemCmdletProvider: ItemExists

        #region ItemCmdletProvider: IsValidPath

        protected override bool IsValidPath(string path)
        {
            return ExecuteAndLog(() => true, nameof(IsValidPath), path);
        }

        #endregion ItemCmdletProvider: IsValidPath

        #region ContainerCmdletProvider : GetChildItems

        protected override void GetChildItems(string path, bool recurse)
        {
            ExecuteAndLog(() => DoGetChildItems(path, recurse), nameof(GetChildItems), path, recurse.ToString());
        }

        private void DoGetChildItems(string path, bool recurse)
        {
            var nodeFactory = GetNodeFactoryFromPath(path, false);
            if (!nodeFactory.Any())
            {
                return;
            }

            nodeFactory.ToList().ForEach(f => GetChildItems(path, f, recurse));
        }

        private void GetChildItems(string path, PathNode pathNode, bool recurse)
            => WriteChildItem(CreateContext(path), path, recurse, pathNode.GetChildNodes(CreateContext(path, recurse)));

        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
            => ExecuteAndLog(() => DoGetChildItemsDynamicParameters(path), nameof(GetChildItemsDynamicParameters), path, recurse.ToString());

        private object DoGetChildItemsDynamicParameters(string path)
        {
            PathNode pathNode = GetFirstNodeFactoryFromPath(path);
            if (null == pathNode)
            {
                return null;
            }

            return ((IGetChildItem)pathNode).GetChildItemParameters;
        }

        private void WriteChildItem(IProviderContext providerContext, string path, bool recurse, IEnumerable<PathNode> children)
        {
            if (null == children)
            {
                return;
            }

            children.ToList().ForEach(pathNode =>
            {
                try
                {
                    var childPath = MakePath(path, pathNode.Name);
                    WritePathNodeAsPSObject(providerContext, childPath, pathNode);
                    if (recurse)
                    {
                        var context = CreateContext(path, recurse);
                        var kids = pathNode.GetChildNodes(context);
                        WriteChildItem(providerContext, childPath, recurse, kids);
                    }
                }
                catch (PipelineStoppedException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    WriteDebug("An exception was raised while writing child items to the pipeline: " + e.ToString());
                }
            });
        }

        #endregion ContainerCmdletProvider : GetChildItems

        private bool IsRootPath(string path) => string.IsNullOrEmpty(Regex.Replace(path.ToLower(), @"[a-z0-9_]+:[/\\]?", ""));

        protected override void GetChildNames(string path, ReturnContainers returnContainers)
        {
            Action a = () => DoGetChildNames(path, returnContainers);
            ExecuteAndLog(a, "GetChildNames", path, returnContainers.ToString());
        }

        private void DoGetChildNames(string path, ReturnContainers returnContainers)
        {
            var nodeFactory = GetNodeFactoryFromPath(path, false);
            if (!nodeFactory.Any())
            {
                return;
            }

            nodeFactory.ToList().ForEach(f => GetChildNames(path, f, returnContainers));
        }

        private void GetChildNames(string path, PathNode pathNode, ReturnContainers returnContainers)
        {
            pathNode
                .GetChildNodes(CreateContext(path))
                .ToList()
                .ForEach(pathNode => WriteItemObject(pathNode.Name, path + "\\" + pathNode.Name, pathNode.IsContainer));
        }

        protected override object GetChildNamesDynamicParameters(string path)
        {
            Func<object> a = () => DoGetChildNamesDynamicParameters(path);
            return ExecuteAndLog(a, "GetChildNamesDynamicParameters", path);
        }

        private object DoGetChildNamesDynamicParameters(string path)
        {
            PathNode pathNode = GetFirstNodeFactoryFromPath(path);
            if (null == pathNode)
            {
                return null;
            }

            return ((IGetChildItem)pathNode).GetChildItemParameters;
        }

        protected override void RenameItem(string path, string newName)
        {
            Action a = () => DoRenameItem(path, newName);
            ExecuteAndLog(a, "RenameItem", path, newName);
        }

        private void DoRenameItem(string path, string newName)
        {
            var factory = GetNodeFactoryFromPath(path);
            if (null == factory || !factory.Any())
            {
                return;
            }

            factory.ToList().ForEach(a => RenameItem(path, newName, a));
        }

        private void RenameItem(string path, string newName, PathNode factory)
        {
            var rename = factory as IRenameItem;
            if (null == factory || null == rename)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.RenameItem, RenameItemNotsupportedErrorID);
                return;
            }

            var fullPath = path;

            if (!ShouldProcess(fullPath, ProviderCmdlet.RenameItem))
            {
                return;
            }

            var child = GetChildName(path);
            try
            {
                rename.RenameItem(CreateContext(fullPath), child, newName);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, RenameItemInvokeErrorID, fullPath);
            }
        }

        protected override object RenameItemDynamicParameters(string path, string newName)
        {
            Func<object> a = () => DoRenameItemDynamicParameters(path);
            return ExecuteAndLog(a, "RenameItemDynamicParameters", path);
        }

        private object DoRenameItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var rename = factory as IRenameItem;
            if (null == factory || null == rename)
            {
                return null;
            }
            return rename.RenameItemParameters;
        }

        #region ContainerCmdletProvider: NewItem

        protected override void NewItem(string path, string itemTypeName, object newItemValue)
            => ExecuteAndLog(() => DoNewItem(path, itemTypeName, newItemValue), nameof(NewItem), itemTypeName, newItemValue.ToArgString());

        private void DoNewItem(string path, string itemTypeName, object newItemValue)
        {
            GetNodeFactoryFromPathOrParent(path, out var isParentOfPath)
                .ToList()
                .ForEach(f => NewItem(path, isParentOfPath, f, itemTypeName, newItemValue));
        }

        private void NewItem(string path, bool isParentPathNodeFactory, PathNode factory, string itemTypeName, object newItemValue)
        {
            var newItemFactory = factory as INewItem;
            if (newItemFactory is null)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.NewItem, NewItemNotSupportedErrorID);
                return;
            }

            var newItemPath = (
                full: path,
                parent: path,
                childName: isParentPathNodeFactory ? GetChildName(path) : null
            );

            if (!isParentPathNodeFactory && string.IsNullOrEmpty(newItemPath.childName))
            {
                WriteGeneralCmdletError(new InvalidOperationException($"item {newItemPath.full} already exists"), NewItemInvokeErrorID, path);
                return;
            }

            if (newItemPath.childName is { })
            {
                newItemPath.parent = GetParentPath(newItemPath.full, GetRootPath());
            }

            if (!ShouldProcess(newItemPath.full, ProviderCmdlet.NewItem))
            {
                return;
            }

            try
            {
                var providerContext = CreateContext(newItemPath.full);
                var newPathNode = newItemFactory.NewItem(providerContext, newItemPath.childName, itemTypeName, newItemValue);

                WritePathNode(providerContext, newItemPath.parent, newPathNode);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, NewItemInvokeErrorID, newItemPath.full);
            }
        }

        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            Func<object> a = () => DoNewItemDynamicParameters(path);
            return ExecuteAndLog(a, "NewItemDynamicParameters", path, itemTypeName, newItemValue.ToArgString());
        }

        private object DoNewItemDynamicParameters(string path)
        {
            var factory = GetNodeFactoryFromPathOrParent(path, out var _).FirstOrDefault();
            var newItemFactory = factory as INewItem;
            if (null == factory || null == newItemFactory)
            {
                return null;
            }

            return newItemFactory.NewItemParameters;
        }

        #endregion ContainerCmdletProvider: NewItem

        protected string GetRootPath()
        {
            if (null != PSDriveInfo)
            {
                return PSDriveInfo.Root;
            }
            return String.Empty;
        }

        protected override void RemoveItem(string path, bool recurse)
            => ExecuteAndLog(() => DoRemoveItem(path, recurse), nameof(RemoveItem), path, recurse.ToString());

        private void DoRemoveItem(string path, bool recurse)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                WriteError(
                 new ErrorRecord(
                     new ItemNotFoundException(path),
                     RemoveItemTargetDoesNotExistErrorID,
                     ErrorCategory.ObjectNotFound,
                     path
                    )
                );
                return;
            }
            factories.ToList().ForEach(f => RemoveItem(path, f, recurse));
        }

        private void RemoveItem(string path, PathNode factory, bool recurse)
        {
            var remove = factory as IRemoveItem;
            if (null == factory || null == remove)
            {
                WriteCmdletNotSupportedAtNodeError(path, ProviderCmdlet.RemoveItem, RemoveItemNotSupportedErrorID);
                return;
            }

            if (!ShouldProcess(path, ProviderCmdlet.RemoveItem))
            {
                return;
            }

            try
            {
                DoRemoveItem(path, recurse, remove);
            }
            catch (Exception e)
            {
                WriteGeneralCmdletError(e, RemoveItemInvokeErrorID, path);
            }
        }

        private void DoRemoveItem(string path, bool recurse, IRemoveItem remove)
        {
            var fullPath = path;
            path = this.GetChildName(path);
            remove.RemoveItem(CreateContext(fullPath, recurse), path);
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            Func<object> a = () => DoRemoveItemDynamicParameters(path);
            return ExecuteAndLog(a, "RemoveItemDynamicParameters", path, recurse.ToString());
        }

        private object DoRemoveItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var remove = factory as IRemoveItem;
            if (null == factory || null == remove)
            {
                return null;
            }

            return remove.RemoveItemParameters;
        }

        protected override bool HasChildItems(string path)
            => ExecuteAndLog(() => DoHasChildItems(path), nameof(HasChildItems), path);

        private bool DoHasChildItems(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            if (null == factory)
            {
                return false;
            }
            var nodes = factory.GetChildNodes(CreateContext(path));
            if (null == nodes)
            {
                return false;
            }
            return nodes.Any();
        }

        #region ContainerCmdletProvider: CopyItem

        protected override void CopyItem(string path, string copyPath, bool recurse)
            => ExecuteAndLog(() => DoCopyItem(path, copyPath, recurse), nameof(CopyItem), path, copyPath, recurse.ToString());

        private void DoCopyItem(string sourceItemPath, string destinationItemPath, bool recurse)
        {
            var sourceNodes = GetNodeFactoryFromPath(sourceItemPath).ToList();
            if (sourceNodes.Any())
            {
                sourceNodes.ToList().ForEach(n => CopyItem(sourceItemPath, n, destinationItemPath, recurse));
            }
            else
            {
                WriteError(new ErrorRecord(
                    new ItemNotFoundException(sourceItemPath),
                    CopyItemSourceDoesNotExistErrorID,
                    ErrorCategory.ObjectNotFound,
                    sourceItemPath
                ));
            }
        }

        private void CopyItem(string sourceItemPath, PathNode sourcePathNode, string destinationItemPath, bool recurse)
        {
            if (sourcePathNode is ICopyItem sourcePathNodeCopy)
            {
                if (!ShouldProcess(sourceItemPath, ProviderCmdlet.CopyItem))
                {
                    return;
                }

                try
                {
                    DoCopyItem(sourceItemPath, destinationItemPath, recurse, sourcePathNodeCopy);
                }
                catch (Exception e)
                {
                    WriteGeneralCmdletError(e, CopyItemInvokeErrorID, sourceItemPath);
                }
            }
            else WriteCmdletNotSupportedAtNodeError(sourceItemPath, ProviderCmdlet.CopyItem, CopyItemNotSupportedErrorID);
        }

        private void DoCopyItem(string sourceItemPath, string destinationItemPath, bool recurse, ICopyItem copyItem)
        {
            var destinationContainerNode = GetNodeFactoryFromPathOrParent(destinationItemPath, out var targetNodeIsParentNode).FirstOrDefault();

            if (destinationContainerNode is null)
            {
                WriteError(new ErrorRecord(
                    new CopyOrMoveToNonexistentContainerException(destinationItemPath),
                    CopyItemDestinationContainerDoesNotExistErrorID,
                    ErrorCategory.WriteError,
                    destinationItemPath
                ));
            }

            var sourceItemName = GetChildName(sourceItemPath);
            var destinationItemName = targetNodeIsParentNode ? GetChildName(destinationItemPath) : null;
            //var destinationContainerPath = targetNodeIsParentNode ? GetParentPath(destinationItemPath, GetRootPath()) : destinationItemPath;

            copyItem.CopyItem(CreateContext(sourceItemPath, recurse), sourceItemName, destinationItemName, destinationContainerNode);
        }

        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
            => ExecuteAndLog(() => DoCopyItemDynamicParameters(path), nameof(CopyItemDynamicParameters), path, destination, recurse.ToString());

        private object DoCopyItemDynamicParameters(string path)
        {
            var factory = GetFirstNodeFactoryFromPath(path);
            var copy = factory as ICopyItem;
            if (null == factory || null == copy)
            {
                return null;
            }

            return copy.CopyItemParameters;
        }

        #endregion ContainerCmdletProvider: CopyItem

        public IContentReader GetContentReader(string path)

        {
            Func<IContentReader> a = () => DoGetContentReader(path);
            return ExecuteAndLog(a, "GetContentReader", path);
        }

        private IContentReader DoGetContentReader(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                WriteError(
                    new ErrorRecord(
                        new ItemNotFoundException(path),
                        GetContentTargetDoesNotExistErrorID,
                        ErrorCategory.ObjectNotFound,
                        path
                    )
                );

                return null;
            }

            return GetContentReader(path, factories.First(a => a is IGetItemContent));
        }

        private IContentReader GetContentReader(string path, PathNode pathNode)
        {
            var getContentReader = pathNode as IGetItemContent;
            if (null == getContentReader)
            {
                return null;
            }

            return getContentReader.GetContentReader(CreateContext(path));
        }

        public object GetContentReaderDynamicParameters(string path)
        {
            Func<object> a = () => DoGetContentReaderDynamicParameters(path);
            return ExecuteAndLog(a, "GetContentReaderDynamicParameters", path);
        }

        private object DoGetContentReaderDynamicParameters(string path)
        {
            var factories = GetFirstNodeFactoryFromPath(path);
            if (null == factories)
            {
                return null;
            }

            var getContentReader = factories as IGetItemContent;
            if (null == getContentReader)
            {
                return null;
            }

            return getContentReader.GetContentReaderDynamicParameters(CreateContext(path));
        }

        public IContentWriter GetContentWriter(string path)
        {
            Func<IContentWriter> a = () => DoGetContentWriter(path);
            return ExecuteAndLog(a, "GetContentWriter", path);
        }

        private IContentWriter DoGetContentWriter(string path)
        {
            var factories = GetNodeFactoryFromPath(path);
            if (null == factories)
            {
                WriteError(
                    new ErrorRecord(
                        new ItemNotFoundException(path),
                        GetContentTargetDoesNotExistErrorID,
                        ErrorCategory.ObjectNotFound,
                        path
                        )
                    );
                return null;
            }

            return GetContentWriter(path, factories.First(a => a is ISetItemContent));
        }

        private IContentWriter GetContentWriter(string path, PathNode pathNode)
        {
            var getContentWriter = pathNode as ISetItemContent;
            if (null == getContentWriter)
            {
                return null;
            }

            return getContentWriter.GetContentWriter(CreateContext(path));
        }

        public object GetContentWriterDynamicParameters(string path)
        {
            Func<object> a = () => DoGetContentWriterDynamicParameters(path);
            return ExecuteAndLog(a, "GetContentWriterDynamicParameters", path);
        }

        private object DoGetContentWriterDynamicParameters(string path)
        {
            var factories = GetFirstNodeFactoryFromPath(path);
            if (null == factories)
            {
                return null;
            }

            var getContentWriter = factories as ISetItemContent;
            if (null == getContentWriter)
            {
                return null;
            }

            return getContentWriter.GetContentWriterDynamicParameters(CreateContext(path));
        }

        public void ClearContent(string path)
        {
            Action a = () => DoClearContent(path);
            ExecuteAndLog(a, "ClearContent");
        }

        private void DoClearContent(string path)
        {
            var clear = GetFirstNodeFactoryFromPath(path) as IClearItemContent;
            if (null == clear)
            {
                return;
            }

            clear.ClearContent(CreateContext(path));
        }

        public object ClearContentDynamicParameters(string path)
        {
            Func<object> a = () => DoClearContentDynamicParameters(path);
            return ExecuteAndLog(a, "ClearContentDynamicParameters", path);
        }

        private object DoClearContentDynamicParameters(string path)
        {
            var clear = GetFirstNodeFactoryFromPath(path) as IClearItemContent;
            if (null == clear)
            {
                return null;
            }

            return clear.ClearContentDynamicParameters(CreateContext(path));
        }
    }
}