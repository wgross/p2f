using CodeOwls.PowerShell.Paths;
using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider;
using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Reflection;

namespace ProviderFramework_2_TypeProvider
{
    [CmdletProvider("Types", ProviderCapabilities.ShouldProcess)]
    public class TypeProvider : Provider
    {
        protected override IPathResolver PathResolver
        {
            get { return new PathResolver(); }
        }

        protected override System.Collections.ObjectModel.Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            var driveInfo = new PSDriveInfo("Types", ProviderInfo, String.Empty, "Provider for loaded .NET assemblies and types", null);
            return new Collection<PSDriveInfo> { new TypeDrive(driveInfo) };
        }
    }

    public class TypeDrive : Drive
    {
        public TypeDrive(PSDriveInfo driveInfo) : base(driveInfo)
        {
        }
    }

    internal class PathResolver : PathResolverBase
    {
        protected override PathNode Root
        {
            get { return new AppDomainPathNode(); }
        }
    }

    internal class AppDomainPathNode : ContainerNode, IGetChildItem
    {
        public IItemProvider GetItemProvider()
        {
            return new ContainerItemProvider(AppDomain.CurrentDomain, Name);
        }

        public override string Name
        {
            get { return "AppDomain"; }
        }

        public override IEnumerable<PathNode> GetChildNodes(CodeOwls.PowerShell.Provider.PathNodeProcessors.IProviderContext providerContext)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   select new AssemblyPathNode(assembly) as PathNode;
        }

        public override PSObject GetItem(IProviderContext providerContext) => this.GetItemProvider().GetItem(providerContext);
    }

    internal class AssemblyPathNode : ContainerNode, IGetChildItem
    {
        private readonly Assembly _assembly;

        public AssemblyPathNode(Assembly assembly)
        {
            _assembly = assembly;
        }

        public IItemProvider GetItemProvider()
        {
            return new ContainerItemProvider(_assembly, Name);
        }

        public override string Name
        {
            get { return _assembly.GetName().Name; }
        }

        public override IEnumerable<PathNode> GetChildNodes(CodeOwls.PowerShell.Provider.PathNodeProcessors.IProviderContext providerContext)
        {
            return from type in _assembly.GetExportedTypes()
                   select new TypePathNode(type) as PathNode;
        }

        public override PSObject GetItem(IProviderContext providerContext) => this.GetItemProvider().GetItem(providerContext);
    }

    internal class TypePathNode : LeafNode
    {
        private readonly Type _type;

        public TypePathNode(Type type)
        {
            _type = type;
        }

        public IItemProvider GetItemProvider()
        {
            return new LeafItemProvider(_type, Name);
        }

        public override string Name
        {
            get { return _type.FullName; }
        }

        public override PSObject GetItem(IProviderContext providerContext) => this.GetItemProvider().GetItem(providerContext);
    }
}