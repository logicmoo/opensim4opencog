using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using OpenMetaverse;
// the Namespace used for all BotClient commands
using MushDLR223.ScriptEngines;

namespace Cogbot.Actions.Search
{
    internal class SearchGroupsCommand : Command, GridMasterCommand
    {
        private AutoResetEvent waitQuery = new AutoResetEvent(false);
        private int resultCount = 0;

        public SearchGroupsCommand(BotClient testClient)
        {
            Name = "searchgroups";
            TheBotClient = testClient;
        }

        public override void MakeInfo()
        {
            Description = "Searches groups.";
            Details = AddUsage(Name + " [search text]", "searches " + Name.Replace("seaches", ""));
            Category = CommandCategory.Groups;
            Parameters =
                CreateParams("searchText", typeof (string), "what you are searching for");
            ResultMap = CreateParams(
                "result", typeof (List<string>), "search results",
                "message", typeof (string), "if success was false, the reason why",
                "success", typeof (bool), "true if command was successful");
        }

        public override CmdResult ExecuteRequest(CmdRequest args)
        {
            // process command line arguments
            if (args.Length < 1)
                return ShowUsage();

            string searchText = string.Empty;
            for (int i = 0; i < args.Length; i++)
                searchText += args[i] + " ";
            searchText = searchText.TrimEnd();

            waitQuery.Reset();

            Client.Directory.DirGroupsReply += Directory_DirGroups;

            // send the request to the directory manager
            Client.Directory.StartGroupSearch(searchText, 0);

            try
            {
                if (waitQuery.WaitOne(20000, false) && Client.Network.Connected)
                {
                    return Success("Your query '" + searchText + "' matched " + resultCount + " Events. ");
                }
                else
                {
                    return Failure("Timeout waiting for simulator to respond.");
                }
            }
            finally
            {
                Client.Directory.DirGroupsReply -= Directory_DirGroups;
            }
        }

        private void Directory_DirGroups(object sender, DirGroupsReplyEventArgs e)
        {
            if (e.MatchedGroups.Count > 0)
            {
                foreach (DirectoryManager.GroupSearchData group in e.MatchedGroups)
                {
                    WriteLine("Group {1} ({0}) has {2} members", group.GroupID, group.GroupName, group.Members);
                }
            }
            else
            {
                WriteLine("Didn't find any groups that matched your query :(");
            }
            waitQuery.Set();
        }
    }
}