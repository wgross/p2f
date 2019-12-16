using CodeOwls.PowerShell.Paths;
using System.Collections.Generic;
using System.Linq;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        private IEnumerable<T> GetPathNodesFromPath<T>(string path) => GetNodeFactoryFromPath(path).OfType<T>();

        private PathNode GetFirstNodeFactoryFromPath(string path) => GetNodeFactoryFromPath(path).FirstOrDefault();

        private IEnumerable<PathNode> GetNodeFactoryFromPath(string path) => GetNodeFactoryFromPath(path, true);

        private IEnumerable<PathNode> GetNodeFactoryFromPath(string path, bool resolveFinalFilter)
        {
            IEnumerable<PathNode> pathNodes = ResolvePath(path);

            if (resolveFinalFilter && !string.IsNullOrEmpty(Filter))
            {
                pathNodes = pathNodes.First().Resolve(CreateContext(path), null);
            }

            return pathNodes;
        }

        private IEnumerable<T> GetPathNodesOrParentFromPath<T>(string path, out bool isParentOfPath)
            => this.GetNodeFactoryFromPathOrParent(path, out isParentOfPath).OfType<T>();

        private IEnumerable<PathNode> GetNodeFactoryFromPathOrParent(string path, out bool isParentOfPath)
        {
            isParentOfPath = false;
            var nodes = ResolvePath(path);

            if (!nodes.Any())
            {
                path = GetParentPath(path, null);
                nodes = ResolvePath(path);

                if (null == nodes || !nodes.Any())
                {
                    //refactor: return null;
                    return Enumerable.Empty<PathNode>();
                }

                isParentOfPath = true;
            }

            if (!string.IsNullOrEmpty(Filter))
            {
                nodes = nodes.First().Resolve(CreateContext(null), null);
            }

            return nodes;
        }
    }
}