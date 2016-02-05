using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.VisualStudio.Services.Agent;
using Microsoft.VisualStudio.Services.Agent.CLI;

namespace Microsoft.VisualStudio.Services.Agent.Tests
{
    public class MockContext : Context, IContext
    {
        public Action<Exception> _Error_E { get; set; }
        public Action<String> _Error_M { get; set; }
        public Action<String, Object[]> _Error_F_A { get; set; }

        public override CancellationToken CancellationToken
        {
            get
            {
                return this.cancellationToken;
            }
        }

        public MockContext()
        {
            // Clear the default production service mappings added by the base constructor.
            this.serviceMappings.Clear();
        }

        public override T GetService<T>()
        {
            // Let the base implementation perform error checking and create a new instance.
            T newObj = base.GetService<T>();

            // Check if a singleton is registered. Otherwise fallback to the new instance already created.
            return (this.serviceInstances[typeof(T)] as T) ?? newObj;
        }

        // Register a singleton for unit testing.
        public void RegisterService<TKey, TValue>(Object instance)
        {
            base.RegisterService<TKey, TValue>(); // Register the type.
            this.serviceInstances[typeof(TKey)] = instance; // Register the singleton.
        }

        public override void Error(Exception ex)
        {
            base.Error(ex);
            if (this._Error_E != null) { this._Error_E(ex); }
        }

        public override void Error(String message)
        {
            base.Error(message);
            if (this._Error_M != null) { this._Error_M(message); }
        }

        public override void Error(String format, params Object[] args)
        {
            base.Error(format, args);
            if (this._Error_F_A != null) { this._Error_F_A(format, args); }
        }

        protected override void Write(LogLevel level, String message)
        {
            // TODO: Consider logging this to somewhere for the test execution. Writing to console causes
            // the output to print to out in the middle of the xunit summary info. This would likely be
            // an issue since xunit executes tests in parallel.
            //Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "[{0}] {1}", level, message));
        }

        private readonly ConcurrentDictionary<Type, Object> serviceInstances = new ConcurrentDictionary<Type, Object>();
        private readonly CancellationToken cancellationToken = new CancellationToken();
    }
}
