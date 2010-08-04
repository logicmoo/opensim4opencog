using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml;
using MushDLR223.ScriptEngines;

namespace RTParser.Utils
{
    internal class TestCaseRunner : XmlNodeEvaluatorImpl
    {
        private Request Loader;
        int passCount;
        int testCount;
        int errorCount;
        int failCount;
        int fudgeCount;
        private bool traceIt;

        public void ResetTests()
        {
            passCount = 0;
            testCount = 0;
            errorCount = 0;
            failCount = 0;
            fudgeCount = 0;
        }

        public TestCaseRunner(Request loader)
            : base("Eval", "_")
        {
            ResetTests();
            Loader = loader;
        }

        public override IEnumerable<XmlNodeEval> GetEvaluators(XmlNode node)
        {
            return base.GetEvaluatorsFromReflection(node);
        }
        public IEnumerable<XmlNode> EvalTestSuite(XmlNode src, Request request, OutputDelegate outputdelegate)
        {
            ResetTests();
            List<XmlNode> list = new List<XmlNode>();
            foreach (var node in src.ChildNodes)
            {
                bool wasTraceIt = traceIt;
                testCount++;
                try
                {
                    bool b;
                    traceIt = false;
                    XmlNode result = RunTest(request, (XmlNode) node, outputdelegate, out b);
                    if (b) passCount++;
                    else
                    {
                        failCount++;
                        if (false)
                        {
                            traceIt = true;
                            result = RunTest(request, (XmlNode) node, outputdelegate, out b);
                        }
                    }
                    if (result != null)
                    {
                        list.Add(result);
                    }
                }
                catch (Exception e)
                {
                    errorCount++;
                    outputdelegate("ERROR in  test " + e);
                }
                finally
                {
                    traceIt = wasTraceIt;
                }
            }
            outputdelegate("passCount={0} failCount={1} testCount={2} errorCount={3} listCount={4} fudgeCount={5} ",
                           passCount, failCount, testCount, errorCount, list.Count, fudgeCount);
            return list;
        }

        /// <summary>
        ///     <TestCase name="connect">
        //        <Input>CONNECT</Input>
        ///       <ExpectedAnswer>Connected to test case AIML set.</ExpectedAnswer>
        ///    </TestCase>
        /// </summary>
        /// <param name="src"></param>
        /// <param name="request"></param>
        /// <param name="outputdelegate"></param>
        /// <returns></returns>
        public IEnumerable<XmlNode> EvalTestCase(XmlNode src, Request request, OutputDelegate outputdelegate)
        {
            try
            {
                bool b;
                XmlNode result = RunTest(request, src, outputdelegate, out b);
                if (result == null) return NO_XmlNode;
                return new[] { result };
            }
            catch (Exception e)
            {
                outputdelegate("ERROR in  test " + e);
                return NO_XmlNode;
            }
        }

        private XmlNode RunTest(Request request, XmlNode src, OutputDelegate outputdelegate, out bool m)
        {
           // request = request ?? Loader.LoaderRequest00;
            User user = request.user;
            var robot = request.TargetBot ?? Loader.TargetBot;

            string tcname = FindNodeOrAttrib(src, "name", null);
            string tcdesc = FindNodeOrAttrib(src, "Description", null);
            string input = FindNodeOrAttrib(src, "Input", null);
            if (input == null)
            {
                outputdelegate("ERROR cannot find 'Input' in '" + src.OuterXml + "'");
                m = false;
                return AIMLTagHandler.getNode("<template type=\"error\">ERROR cannot find 'Input' in '" + src.OuterXml + "'</template>", src);
            }
            string userID = FindNodeOrAttrib(src, "UserId,UserName", () => user.UserID);

            const string MISSING_EXPECTED_ANSWER = "ExpectedKeywords";
            List<string> matchTheseToPass = new List<string>();
            string expectedAnswer = FindNodeOrAttrib(src, "ExpectedAnswer", () => MISSING_EXPECTED_ANSWER);
            expectedAnswer = Fudge(expectedAnswer);
            if (expectedAnswer == MISSING_EXPECTED_ANSWER)
            {
                List<XmlNode> nodes = AIMLLoader.FindNodes("ExpectedKeywords", src);
                if (nodes == null || nodes.Count == 0)
                {
                    outputdelegate("ERROR cannot find 'ExpectedAnswer' in '" + src.OuterXml + "'");
                }
                else
                {
                    foreach (var list in nodes)
                    {

                        string v = Unifiable.InnerXmlText(list);

                        matchTheseToPass.Add(".*" + Fudge(v) + ".*");
                    }
                }

            } else
            {
                matchTheseToPass.Add("^" + Fudge(expectedAnswer) + "$");
            }

            outputdelegate("{0}: {1} ", tcname, tcdesc);
            outputdelegate("{0}: {1} ", userID, input);
            string resp = "ERROR";
            try
            {
                var r = robot.GetRequest(input, userID);
                r.IsTraced = traceIt;
                if (traceIt)
                {
                    RTPBot.Breakpoint("testing...");
                    r.DebugLevel = 9;
                }
                resp = Fudge(robot.Chat(r).Output);
                outputdelegate("{0}: {1} ", robot, resp);
                m = true;
                int good = 0;
                foreach (var s in matchTheseToPass)
                {
                    if (!Matches(resp, s, FindNodeOrAttrib(src, "MatchType,Match", null)))
                    {
                        m = false;
                    } else
                    {
                        good++;
                    }
                }
                outputdelegate("PASSED={0}", m);
                if (traceIt)
                {
                    RTPBot.Breakpoint("tested...");
                }
                return GetMessage(src, "PASSED='" + m +
                                       "'", "TESTCASE='" + tcname + "' GOOD='" + good +
                                            "' RESPNS='" + resp +
                                            "' EXPECT='" + expectedAnswer +
                                            "' INPUT='" + input +
                                            "' DESC='" + tcdesc + "'");
            }
            catch (Exception err)
            {
                string ERRMSG = "" + err;
                m = false;
                errorCount++;
                return GetMessage(src, "PASSED='" + m +
                                       "'", "TESTCASE='" + tcname + "' ERRMSG='" + ERRMSG + "' RESP='" + resp +
                                            "' EXPECT='" + expectedAnswer +
                                            "' INPUT='" + input +
                                            "' DESC='" + tcdesc + "'");
            }
        }

        private XmlNode GetMessage(XmlNode src, string attrs, string msg)
        {
            attrs = attrs.Replace("\"", "#").Replace("'", "\"");
            msg = msg.Replace("\"", "#").Replace("'", "\"");
            return AIMLTagHandler.getNode("<template " + attrs + " >" + attrs + " " + msg + "</template>", src);
        }

        bool Matches(string resp, string answer, string s)
        {
            if (resp == answer) return true;
            if ((new Regex(answer)).IsMatch(resp))
            {
                return true;
            }
            if (answer.StartsWith("^") && answer.EndsWith("$"))
            {
                answer = answer.Substring(1, answer.Length - 2);
            }
            if (Fudge(resp) == Fudge(answer))
            {
                if (!traceIt) fudgeCount++;
                return true;
            }
            return false;
        }

        private static string Fudge(string expectedAnswer)
        {
            expectedAnswer = AIMLLoader.CleanPunct(AIMLLoader.CleanWhitepaces(expectedAnswer));
            expectedAnswer = expectedAnswer.Replace("<html:", "<");
            string was = expectedAnswer;
            expectedAnswer = expectedAnswer.Replace("<br xmlns:html=\"http://www.w3.org/1999/xhtml\"/>", " ");
            expectedAnswer = expectedAnswer.Replace("<br/>", " ");
            if (was != expectedAnswer)
            {
                expectedAnswer = expectedAnswer.Replace(".", " ");
                return AIMLLoader.CleanWhitepaces(expectedAnswer);                
            }
            expectedAnswer = expectedAnswer.Replace(".", " ");
            return AIMLLoader.CleanWhitepaces(expectedAnswer);
        }


        static string FindNodeOrAttrib(XmlNode myNode, string names, Func<string> defaultNotFound)
        {
            const string attribNotFOund = "ATTRIB_NOT_FOUND";
            string value = RTPBot.GetAttribValue(myNode, names, attribNotFOund);
            if (value == attribNotFOund)
            {
                XmlNode holder = AIMLLoader.FindNode(names, myNode, null);
                if (holder != null)
                {
                    value = Unifiable.InnerXmlText(holder);
                    return value;
                }
                return defaultNotFound == null ? null : defaultNotFound();
            }
            return value;
        }


        public override string ToString()
        {
            return GetType().Name;
        }
    }
}