using System;
using System.Collections.Generic;
using System.Text;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using OpenMetaverse; //using libsecondlife;

namespace Cogbot
{
    abstract public class AListener : Listener, IDisposable
    {
        //   protected ClientManager botclient;
        public BotClient client;

        public AListener(BotClient _parent)
        {
            //botclient = _parent;
            client = _parent;//.CurrentClient;
            Dictionary<string, Listener> clientPlugins = client.Plugins;
            lock (clientPlugins) clientPlugins[this.GetModuleName()] = this;
        }

        /// <summary>
        ///  Name registered in the BotClient.registrationTypes collection
        /// </summary>
        /// <returns></returns>
        public abstract string GetModuleName();

        public abstract void StartupListener();

        public virtual void InvokeCommand(string cmd, OutputDelegate output)
        {
            output("NotImplemented: " + this + " " + cmd);
        }

        public abstract void Dispose();
    }

    public interface Listener : IDisposable, ContextualSingleton
    {
        string GetModuleName();
        void StartupListener();
        void InvokeCommand(string cmd, OutputDelegate output);
    }
}
