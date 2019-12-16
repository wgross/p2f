﻿using CodeOwls.PowerShell.Paths;

namespace ProviderFramework_1_TheNullProvider
{
    /// <summary>
    /// the value factory class.
    ///
    /// used by P2F to manage items for a
    /// particular path value.
    /// </summary>
    internal class NullRootPathNode : PathNode
    {
        private const string NodeName = "NullRootNode";

        /// <summary>
        /// supplies the item for the current path value
        ///
        /// the item it wrapped in either a PathValue instance
        /// that describes the item, its name, and whether it is
        /// a container.
        /// </summary>
        /// <seealso cref="PathValue"/>
        /// <seealso cref="LeafPathValue"/>
        /// <seealso cref="ContainerPathValue"/>
        public override IItemProvider GetItemProvider()
        {
            var item = new NullItem();

            return new LeafItemProvider(item, Name);
        }

        /// <summary>
        /// supplies the name for the item at the current path value
        /// </summary>
        public override string Name
        {
            get { return NullRootPathNode.NodeName; }
        }
    }
}