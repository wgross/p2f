using CodeOwls.PowerShell.Provider.PathNodeProcessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace CodeOwls.PowerShell.Provider
{
    public partial class Provider
    {
        public ProviderMethodCall StepInProviderMethod(string providerPath, [CallerMember] string memberName = default)
        {
            var tmp = new ProviderMethodCall(this, this.CreateContext(providerPath), memberName ?? string.Empty);
            tmp.LogStepIn(providerPath);
            return tmp;
        }
    }

    public readonly struct ProviderMethodCall : IDisposable
    {
        private readonly string memberName;

        public ProviderMethodCall(Provider provider, IProviderContext providerContext, string memberName)
        {
            this.Provider = provider;
            this.ProviderContext = providerContext;
            this.memberName = memberName;
        }

        public Provider Provider { get; }

        public IProviderContext ProviderContext { get; }

        public void Dispose() => this.LogStepOut();

        internal CapabilityCall<T> Resolve<T>(string path)
        {
            return new CapabilityCall<T>(this, this.Provider.GetPathNodesFromPath<T>(path));
        }

        private void LogStepOut() => this.Provider.WriteDebug($"StepOut:{this.memberName}(path='{this.ProviderContext.Path}')");

        internal void LogStepIn(string path) => this.Provider.WriteDebug($"StepIn:{this.memberName}(path='{this.ProviderContext.Path}')");
    }

    public readonly struct CapabilityCall<T>
    {
        private readonly ProviderMethodCall methodCall;
        private readonly IEnumerable<T> capabilities;

        public CapabilityCall(ProviderMethodCall methodCall, IEnumerable<T> capabilities)
        {
            this.methodCall = methodCall;
            this.capabilities = capabilities ?? Enumerable.Empty<T>();
        }

        public void InvokeWithContext(Action<IProviderContext, IEnumerable<T>> invocation)
        {
            try
            {
                if (this.capabilities.Any())
                {
                    invocation?.Invoke(this.methodCall.ProviderContext, this.capabilities);
                }
                else
                {
                    this.methodCall.Provider.WriteError(new ErrorRecord(
                        exception: new NotSupportedException($"Capability({typeof(T).Name}) not supported at path='{this.methodCall.ProviderContext.Path}')"),
                        errorId: "CmdletNotSupported",
                        errorCategory: ErrorCategory.NotImplemented,
                        targetObject: this.methodCall.ProviderContext.Path));
                }
            }
            catch (Exception ex)
            {
                this.methodCall
                    .Provider
                    .WriteDebug($"Error:Capability({typeof(T).Name}) at path = '{this.methodCall.ProviderContext.Path}' threw {ex}");
                throw;
            }
        }
    }
}