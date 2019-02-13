using System;

namespace Agent.Plugins.Log.TestResultParser.Plugin
{
    public interface IBus<out TMessage>
    {
        /// <summary>
        /// Subscribe to Message Bus to receive messages via Pub-Sub model
        /// </summary>
        Guid Subscribe(Action<TMessage> handlerAction);

        /// <summary>
        /// Unsubscribe to Message Bus so that subscriber no longer receives messages
        /// </summary>
        void Unsubscribe(Guid subscriptionId);
    }
}
