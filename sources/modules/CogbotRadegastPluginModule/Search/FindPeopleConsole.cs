using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using OpenMetaverse;
//using SLNetworkComm;
using METAboltInstance = Radegast.RadegastInstance;
using SLNetCom = Radegast.Netcom.RadegastNetcom;

namespace METAbolt
{
    public partial class FindPeopleConsole : UserControl
    {
        private METAboltInstance instance;
        private SLNetCom netcom;
        private GridClient client;

        private UUID queryID = UUID.Zero;
        private Dictionary<string, UUID> findPeopleResults;

        public event EventHandler SelectedIndexChanged;

        public FindPeopleConsole(METAboltInstance instance, UUID queryID)
        {
            InitializeComponent();

            findPeopleResults = new Dictionary<string, UUID>();
            this.queryID = queryID;

            this.instance = instance;
            netcom = this.instance.Netcom;
            client = this.instance.Client;
            AddClientEvents();
        }

        private void AddClientEvents()
        {
            client.Directory.DirPeopleReply += Directory_OnDirPeopleReply;
        }

        private void Directory_OnDirPeopleReply(object sender, DirPeopleReplyEventArgs e)
        {
            BeginInvoke(new MethodInvoker(() =>
            {
                PeopleReply(e.QueryID, e.MatchedPeople);
            }));
        }

        //UI thread
        private void PeopleReply(UUID queryID, List<DirectoryManager.AgentSearchData> matchedPeople)
        {
            if (queryID != this.queryID) return;

            lvwFindPeople.BeginUpdate();

            foreach (DirectoryManager.AgentSearchData person in matchedPeople)
            {
                string fullName = person.FirstName + " " + person.LastName;
                findPeopleResults.Add(fullName, person.AgentID);

                ListViewItem item = lvwFindPeople.Items.Add(fullName);
                item.SubItems.Add(person.Online ? "Yes" : "No");
            }

            lvwFindPeople.Sort();
            lvwFindPeople.EndUpdate();
            pPeople.Visible = false;  
        }

        public void ClearResults()
        {
            findPeopleResults.Clear();
            lvwFindPeople.Items.Clear();
        }

        private void lvwFindPeople_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnSelectedIndexChanged(e);
        }

        protected virtual void OnSelectedIndexChanged(EventArgs e)
        {
            if (SelectedIndexChanged != null) SelectedIndexChanged(this, e);
        }

        public Dictionary<string, UUID> LLUUIDs
        {
            get { return findPeopleResults; }
        }

        public UUID QueryID
        {
            get { return queryID; }
            set { queryID = value; }
        }

        public int SelectedIndex
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return -1;
                if (lvwFindPeople.SelectedItems.Count == 0) return -1;

                return lvwFindPeople.SelectedIndices[0];
            }
        }

        public string SelectedName
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return string.Empty;
                if (lvwFindPeople.SelectedItems.Count == 0) return string.Empty;

                return lvwFindPeople.SelectedItems[0].Text;
            }
        }

        public bool SelectedOnlineStatus
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return false;
                if (lvwFindPeople.SelectedItems.Count == 0) return false;

                string yesNo = lvwFindPeople.SelectedItems[0].SubItems[0].Text;

                if (yesNo == "Yes")
                    return true;
                else if (yesNo == "No")
                    return false;
                else
                    return false;
            }
        }

        public UUID SelectedAgentUUID
        {
            get
            {
                if (lvwFindPeople.SelectedItems == null) return UUID.Zero;
                if (lvwFindPeople.SelectedItems.Count == 0) return UUID.Zero;

                string name = lvwFindPeople.SelectedItems[0].Text;
                return findPeopleResults[name];
            }
        }
    }
}
