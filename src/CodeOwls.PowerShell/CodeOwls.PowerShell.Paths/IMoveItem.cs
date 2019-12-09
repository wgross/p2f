﻿using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Paths
{
    public interface IMoveItem
    {
        object MoveItemParameters => new RuntimeDefinedParameterDictionary();

        IItemProvider MoveItem(IProviderContext providerContext, string path, string movePath, IItemProvider destinationContainer);
    }
}