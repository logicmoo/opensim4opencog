﻿using System;
using System.Runtime;
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using RTParser;
using RTParser.Utils;

namespace RTParser.AIMLTagHandlers
{
    public class setevidence : RTParser.Utils.AIMLTagHandler
    {

        public setevidence(RTParser.RTPBot bot,
                RTParser.User user,
                RTParser.Utils.SubQuery query,
                RTParser.Request request,
                RTParser.Result result,
                XmlNode templateNode)
            : base(bot, user, query, request, result, templateNode)
        {
        }



        protected override Unifiable ProcessChange()
        {
            if (this.templateNode.Name.ToLower() == "setevidence")
            {
                try
                {
                    string name = GetAttribValue("evidence", null);
                    string cur_prob_str = GetAttribValue("prob", "0.1");
                    double cur_prob = double.Parse(cur_prob_str);

                    this.user.bot.pMSM.setEvidence(name, cur_prob);
                }
                catch
                {

                }
            }
            return Unifiable.Empty;

        }
    }
}