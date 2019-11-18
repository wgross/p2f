# p2f
The PowerShell Provider Framework (P2F) performs the heavy lifting for developing PowerShell Providers.

# Major Changes in this Fork

* Uses .Net Core 3
* Adds default methods to internal interfaces to returs a default parameter set for the cmdlets
* avoid to return null for dynamic parameters.
* properties of PSObject are now retrieved or set by efault methods of IPathNode. This allows to override the default behaviour of p2f using the TypeDescriptor. I needed this to implement my dynamic data model.
* ICopyItem doesn't return the item created by copy since powershell dosn't write it to te pipe anyway.

Since some of my changes are incompatible I'm not planning to a make a pull request to the original repo from it. I'm think also that i will deviate from the original implementation even more in the future. 




