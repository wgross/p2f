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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Provider.PathNodes
{
    /// <summary>
    /// Provides an item for output to the powershell processing pipe.
    /// The item is provided completely fro <see cref="GetItem"/> or as properties collection from
    /// <see cref="GetItemProperties(IEnumerable{string})"/>
    /// </summary>
    public interface IItemProvider
    {
        string Name { get; }

        bool IsContainer { get; }

        object GetItem();

        IEnumerable<PSPropertyInfo> GetItemProperties(IEnumerable<string> propertyNames) => GetItemProperties(this, propertyNames);

        #region GetItemProperties default iomplementation

        static IEnumerable<PSPropertyInfo> GetItemProperties(IItemProvider thisPathValue, IEnumerable<string> propertyNames)
        {
            if (propertyNames is null)
            {
                yield break;
            }

            var thisItem = thisPathValue.GetItem();
            var propDescs = TypeDescriptor.GetProperties(thisItem);
            var props = (from PropertyDescriptor prop in propDescs
                         where (propertyNames.Contains(prop.Name, StringComparer.InvariantCultureIgnoreCase))
                         select prop);

            foreach (var p in props)
            {
                var iv = p.GetValue(thisItem);
                if (null != iv)
                {
                    yield return new PSNoteProperty(p.Name, iv);
                }
            };
        }

        #endregion GetItemProperties default iomplementation

        void SetItemProperties(IEnumerable<PSPropertyInfo> properties) => SetItemProperties(this, properties);

        #region SetItemProperties default implementation

        static void SetItemProperties(IItemProvider thisPathValue, IEnumerable<PSPropertyInfo> properties)
        {
            var nodeItem = thisPathValue.GetItem();
            var propDescs = TypeDescriptor.GetProperties(nodeItem);
            var props = (from PropertyDescriptor propDesc in propDescs
                         let psod = (from pso in properties
                                     where StringComparer.InvariantCultureIgnoreCase.Equals(pso.Name, propDesc.Name)
                                     select pso).FirstOrDefault()
                         where null != psod
                         select new { PSProperty = psod, Property = propDesc });

            props.ToList().ForEach(p => p.Property.SetValue(nodeItem, p.PSProperty.Value));
        }

        #endregion SetItemProperties default implementation
    }
}