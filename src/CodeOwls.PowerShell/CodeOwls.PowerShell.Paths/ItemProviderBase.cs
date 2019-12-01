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

using CodeOwls.PowerShell.Paths.Extensions;

namespace CodeOwls.PowerShell.Provider.PathNodes
{
    public class ContainerItemProvider : ItemProviderBase
    {
        public ContainerItemProvider(object item, string name) : base(item, name, isContainer: true)
        {
        }
    }

    public class LeafItemProvider : ItemProviderBase
    {
        public LeafItemProvider(object item, string name) : base(item, name, isContainer: false)
        {
        }
    }

    public abstract class ItemProviderBase : IItemProvider
    {
        private readonly object item;
        private readonly string name;

        public ItemProviderBase(object item, string name, bool isContainer)
        {
            this.item = item;
            this.name = name;
            this.IsContainer = isContainer;
        }

        public object GetItem() => this.item;

        public string Name => this.name.MakeSafeForPath();

        public bool IsContainer { get; }
    }
}