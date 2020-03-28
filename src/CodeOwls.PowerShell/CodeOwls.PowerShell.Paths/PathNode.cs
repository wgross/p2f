/*
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

using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public abstract class PathNode : IGetItem, IGetItemProperty, IGetChildItem
    {
        public virtual IEnumerable<PathNode> Resolve(IProviderContext providerContext, string nodeName)
        {
            var children = GetChildNodes(providerContext);
            foreach (var child in children)
            {
                if (null == nodeName || StringComparer.InvariantCultureIgnoreCase.Equals(nodeName, child.Name))
                {
                    yield return child;
                }
            }
        }

        /// <summary>
        /// The IsContainer property indicates if this node may hold aother nodes as subnodes.
        /// It doesn't indicate if the conatiner has currenty child nodes.
        /// </summary>
        public abstract bool IsContainer { get; }

        /// <summary>
        /// Any path node must provide a name shis represenst it under its parents name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// A path node may implement on e or more capabilities. Every Powershell provioder Cmdlet requires one
        /// to process this node.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public virtual (bool has, T capability) TryGetCapability<T>() where T : class
        {
            if (this is T cability)
                return (true, cability);
            return (false, default);
        }

        #region IGetChildNodes

        public abstract IEnumerable<PathNode> GetChildNodes(IProviderContext providerContext);

        #endregion IGetChildNodes

        #region IGetItem

        public abstract PSObject GetItem(IProviderContext providerContext);

        #endregion IGetItem

        #region IGetItemProperties

        public IEnumerable<PSPropertyInfo> GetItemProperties(IProviderContext providerContext, IEnumerable<string> propertyNames)
        {
            if (propertyNames.Any())
                return this.GetItem(providerContext).Properties.Where(p => propertyNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase));

            return this.GetItem(providerContext).Properties;
        }

        #endregion IGetItemProperties
    }

    public abstract class LeafNode : PathNode
    {
        public override IEnumerable<PathNode> GetChildNodes(IProviderContext providerContext) => Enumerable.Empty<PathNode>();

        public override bool IsContainer => false;
    }

    public abstract class ContainerNode : PathNode
    {
        public override bool IsContainer => true;
    }
}