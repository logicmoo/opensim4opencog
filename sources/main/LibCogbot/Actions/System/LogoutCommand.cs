using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions
{
    internal class Logout : Command, BotSystemCommand
    {
        public Logout(BotClient Client)
            : base(Client)
        {
            Name = "Logout";
        }

        public override void MakeInfo()
        {
            Description = "Logout from grid";
            AddVersion(CreateParams(), "logout the targeted bot");
            Category = CommandCategory.BotClient;
            Parameters = CreateParams();
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            if (Client.Network.Connected)
            {
                ClientManager.Logout(Client);
                return Success("Logged out " + Client);
            }
            return Success("Was Logged out " + Client);
        }
    }
}