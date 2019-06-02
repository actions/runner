using System;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Services.CircuitBreaker
{
    /// <summary>
    /// Command factory for instantiating the commands. This factory is used by the client to instantiate the different commands.
    /// Using factory interface because at WebApi level, we don't have reference to the VSSF SDK and we want to abstract the command generation logic from web-api layer
    /// </summary>
    public interface ICommandFactory
    {
        /// <summary>
        /// Command Key for this instnace.
        /// </summary>
        CommandKey CommandKey { get; }

        /// <summary>
        /// Initializes a new instance of the Command class.
        /// </summary>
        /// <param name="commandSetter">Enables setting command defaults in code.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        /// <param name="continueOnCapturedContext">True to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        /// <returns>New instance of the Command</returns>
        CommandAsync CreateCommandAsync(CommandSetter commandSetter, Func<Task> run = null, Func<Task> fallback = null, bool continueOnCapturedContext = false);

        /// <summary>
        /// Initializes a new instance of the Command class.
        /// </summary>
        /// <param name="commandSetter">Enables setting command defaults in code.</param>
        /// <param name="run">The Run delegate called when the circuit is closed.</param>
        /// <param name="fallback">The Fallback delegate called when the circuit is open.</param>
        /// <param name="continueOnCapturedContext">True to attempt to marshal the continuation back to the original context captured; otherwise, false.</param>
        /// <returns>New instance of the Command</returns>
        CommandAsync<TResult> CreateCommandAsync<TResult>(CommandSetter commandSetter, Func<Task<TResult>> run = null, Func<Task<TResult>> fallback = null, bool continueOnCapturedContext = false);
    }
}
