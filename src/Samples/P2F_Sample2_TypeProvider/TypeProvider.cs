﻿using CodeOwls.PowerShell.Paths.Processors;
using CodeOwls.PowerShell.Provider;
using CodeOwls.PowerShell.Provider.PathNodes;
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
        protected override IPathNode Root
        {
            get { return new AppDomainPathNode(); }
        }
    }

    internal class AppDomainPathNode : PathNodeBase
    {
        public override IItemProvider GetItemProvider()
        {
            return new ContainerItemProvider(AppDomain.CurrentDomain, Name);
        }

        public override string Name
        {
            get { return "AppDomain"; }
        }

        public override IEnumerable<IPathNode> GetNodeChildren(CodeOwls.PowerShell.Provider.PathNodeProcessors.IProviderContext providerContext)
        {
            return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                   select new AssemblyPathNode(assembly) as IPathNode;
        }
    }

    internal class AssemblyPathNode : PathNodeBase
    {
        private readonly Assembly _assembly;

        public AssemblyPathNode(Assembly assembly)
        {
            _assembly = assembly;
        }

        public override IItemProvider GetItemProvider()
        {
            return new ContainerItemProvider(_assembly, Name);
        }

        public override string Name
        {
            get { return _assembly.GetName().Name; }
        }

        public override IEnumerable<IPathNode> GetNodeChildren(CodeOwls.PowerShell.Provider.PathNodeProcessors.IProviderContext providerContext)
        {
            return from type in _assembly.GetExportedTypes()
                   select new TypePathNode(type) as IPathNode;
        }
    }

    internal class TypePathNode : PathNodeBase
    {
        private readonly Type _type;

        public TypePathNode(Type type)
        {
            _type = type;
        }

        public override IItemProvider GetItemProvider()
        {
            return new LeafItemProvider(_type, Name);
        }

        public override string Name
        {
            get { return _type.FullName; }
        }
    }
}