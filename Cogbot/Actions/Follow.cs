using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse; //using libsecondlife;
using System.Threading;
using System.Windows.Forms;
using cogbot.TheOpenSims;

namespace cogbot.Actions
{
    class Follow : Action
    {


        public Follow(BotClient Client)
            : base(Client)
        {
            helpString = "Start or stop following a user.";
            usageString = "To start following an avatar, type \"follow <avatar name>\" \r\n" +
                          "To stop following an avatar, type \"stop-following <avatar name>\"";
        }


        public override void acceptInput(string verb, Parser pargs)
        {
            // base.acceptInput(verb, args);
            string[] args = pargs.tokens;
            UUID primID;
            if (verb == "follow")
            {

                string name = pargs.objectPhrase;
                if (String.IsNullOrEmpty(name.Trim())) name = "avatar";
                Primitive avatar;
                if (WorldSystem.tryGetPrim(name, out avatar))
                {
                    SimObject followAvatar = WorldSystem.GetSimObject(avatar);
                    String str = "" + Client + " start to follow " + followAvatar + ".";
                    WriteLine(str);
                    SimActor me = WorldSystem.TheSimAvatar;

                    // The thread that accepts the Client and awaits messages
                    me.CurrentAction = new FollowerAction(me, followAvatar);
                }
                else
                {
                    WriteLine("I don't know who " + name + " is.");
                }
            }
            else if (verb == "stop-following")
            {


                WriteLine("You stop following " + WorldSystem.TheSimAvatar.ApproachPosition + ".");

                WorldSystem.TheSimAvatar.SetMoveTarget(null,10);
                WorldSystem.TheSimAvatar.StopMoving();
            }
            else
            {
                WriteLine("You aren't following anyone.");
            }

            TheBotClient.describeNext = true;
        }

    }
}
