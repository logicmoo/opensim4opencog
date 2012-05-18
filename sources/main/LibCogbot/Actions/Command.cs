using System;
using System.Collections;
using System.Collections.Generic;
using cogbot.TheOpenSims;
using MushDLR223.ScriptEngines;
using MushDLR223.Utilities;
using OpenMetaverse;
using System.Reflection;
using OpenMetaverse.StructuredData;
using Simulator = OpenMetaverse.Simulator;

namespace cogbot.Actions
{
    public enum CommandCategory : int
    {
        Parcel,
        Appearance,
        Movement,
        Simulator,
        Communication,
        Inventory,
        Objects,
        Voice,
        BotClient,
        Friends,
        Groups,
        Other,
        Unknown,
        Search,
        Money,
        Security
    }
    /// <summary>
    /// Ensure the reflection API is not used to load this command
    /// The command is loaded likely byu the plugin 
    /// </summary>
    public interface NotAutoLoaded : BotCommand
    {
    }
    /// <summary>
    /// An interface for commands is only invoked on Region mastering bots
    /// Such as terrain uploads and simulator info (10 bots doing the command at once will create problems)
    /// Non region master bots are thinner clients and usually not fit for object tracking
    /// </summary>
    public interface RegionMasterCommand : BotCommand
    {
    }
    /// <summary>
    /// An interface for commands is only invoked on Grid mastering bots
    /// Such as Directory info requests (10 bots doing the command at once will create problems)   
    /// </summary>
    public interface GridMasterCommand : BotCommand
    {
    }
    /// <summary>
    /// An interface for commands that do not target any specific bots
    ///  Such as pathsystem maintainance or application commands
    ///  The gridClient used though will be GridMaster
    /// </summary>
    public interface SystemApplicationCommand : BotCommand
    {
    }

    /// <summary>
    /// An interface for commands that do not require a connected grid client
    /// such as Login or settings but still targets each bot individually
    /// </summary>
    public interface BotSystemCommand : BotCommand
    {
    }

    /// <summary>
    /// An interface for commands that DO REQUIRE a connected grid client
    /// such as say,jump,movement
    /// </summary>    
    public interface BotPersonalCommand : BotCommand
    {
    }
    /// <summary>
    /// An interface for commands that are not recreated per call instance
    /// </summary>
    public interface BotStatefullCommand : BotCommand, IDisposable
    {
    }
    public interface BotCommand
    {
    }

    public class  CommandInfo
    {
        public bool IsStateFull;
        public CommandCategory Category;
        public string Name { get; set; }
        public string helpString;
        public string usageString;
        public bool IsGridClientCommnad = false;
        /// <summary>
        /// Introspective Parameters for calling command from code
        /// </summary>
        public NamedParam[] Parameters;

        public Type CmdType;
        public Command WithBotClient;
        public ConstructorInfo CmdTypeConstructor;
        public string Description
        {
            get
            {
                if (!string.IsNullOrEmpty(helpString)) return helpString;
                return helpString + "  Usage: " + usageString; 
            }
        }

        public CommandInfo(Command live)
        {
            LoadFromCommand(live);
        }
        public void LoadFromCommand(Command live)
        {
            IsStateFull = live.IsStateFull || live is BotStatefullCommand;
            Name = live.Name;
            usageString = live.Usage;
            Parameters = live.Parameters;
            helpString = live.Description;
            Category = live.Category;
            CmdType = live.GetType();
            CmdTypeConstructor = CmdType.GetConstructors()[0];
            IsGridClientCommnad = CmdTypeConstructor.GetParameters()[0].ParameterType == typeof(GridClient);
            if (IsStateFull) WithBotClient = live;
        }

        public Command MakeInstance(BotClient client)
        {
            if (WithBotClient != null)
            {
                if (WithBotClient.TheBotClient == client)
                {
                    return WithBotClient;
                }
            }
            var cmd = (Command) CmdTypeConstructor.Invoke(new object[] {client});
            cmd.TheBotClient = client;
            return cmd;
        }

    }

    public abstract class Command : IComparable
    {
        public bool IsStateFull;
        public CommandCategory Category;
        public string Name { get; set; }
        protected string helpString;
        protected string usageString;

        virtual public string Description
        {
            get
            {
                return helpString + "  Usage: " + Usage;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) return;
                int half = value.ToLower().IndexOf("usage");
                if (half == -1)
                {
                    half = value.ToLower().IndexOf("use");
                }
                if (half == -1)
                {
                    helpString = value;
                    return;
                }
                helpString = value.Substring(0, half).TrimEnd();
                Usage = value.Substring(half);

            }
        }

        public virtual string Usage
        {
            get { return usageString; }
            set
            {
                usageString = value.Trim().Replace("Usage:", " ").Replace("usage:", " ").Replace("Use:", " ").Trim();
            }
        }

        /// <summary>
        /// Introspective Parameters for calling command from code
        /// </summary>
        public NamedParam[] Parameters;
        public NamedParam[] ResultMap;

        /// <summary>
        /// Show commandusage
        /// </summary>
        /// <returns>CmdResult Failure with a string containing the parameter usage instructions</returns>
        public virtual CmdResult ShowUsage()
        {
            return ShowUsage(Usage);
        }
        public virtual CmdResult ShowUsage(string usg)
        {
            CmdResult res = Failure("Usage: //" + usg);
            res.InvalidArgs = true;
            return res;
        }

        protected T GetParamValue<T>(string paramName, Parser parser)
        {
            foreach (NamedParam param in Parameters)
            {

            }
            return default(T);
        }



        private OutputDelegate _writeLine;
        public UUID CallerID = UUID.Zero;
        public OutputDelegate WriteLine
        {
            get
            {
                if (_writeLine == null)
                {
                    return StaticWriteLine;
                }
                return _writeLine;
            }
            set
            {
                if (value == null)
                {
                    _writeLine = StaticWriteLine;
                    return;
                }
                _writeLine = value;
            }
        }

        private int success = 0, failure = 0;

        public Command()
            : this(null)
        {
            if (this is BotStatefullCommand)
            {
                DLRConsole.DebugWriteLine("" + this + " is not a BotStatefullCommand?!");                
            }
        } // constructor

        private void StaticWriteLine(string s, params object[] args)
        {
            if (_mClient != null) _mClient.WriteLine(Name + ": " + s, args);
        }

        public Command(BotClient bc)
        {
            _mClient = bc;
            WriteLine = StaticWriteLine;
            Name = GetType().Name.Replace("Command", "");
            if (!(this is BotCommand))
            {
                DLRConsole.DebugWriteLine("" + this + " is not a BotCommand?!");
            }
            if (this is BotPersonalCommand)
            {
                Parameters = new[] { new NamedParam(typeof(GridClient), null) };
                Category = CommandCategory.Other;
            }
            if (this is BotSystemCommand)
            {
                Parameters = new[] { new NamedParam(typeof(GridClient), null) };
                Category = CommandCategory.Simulator;
            }
            if (this is RegionMasterCommand)
            {
                Parameters = new[] { new NamedParam(typeof(Simulator), null) };
                Category = CommandCategory.Simulator;
            }
            if (this is SystemApplicationCommand)
            {
                Parameters = new[] { new NamedParam(typeof(GridClient), null) };
                Category = CommandCategory.BotClient;
            }
            if (this.GetType().Namespace.ToString() == "cogbot.Actions.Movement")
            {
                Category = CommandCategory.Movement;
            }
        } // constructor



        /// <summary>
        /// 
        /// </summary>
        /// <param name="verb"></param>
        /// <param name="args"></param>
        public virtual CmdResult acceptInput(string verb, Parser args, OutputDelegate writeLine)
        {
            success = failure = 0;
            WriteLine = writeLine;
            try
            {
                return Execute(args.tokens, CallerID, writeLine);
            }
            catch (Exception e)
            {
                return Failure("" + e);
            }
        } // method: acceptInput



        /// <summary>
        /// When set to true, think will be called.
        /// </summary>
        public bool Active;

        /// <summary>
        /// Called twice per second, when Command.Active is set to true.
        /// </summary>
        public virtual void Think()
        {
        }

        private BotClient _mClient = null;

        public BotClient Client
        {
            get
            {
                return TheBotClient;
            }
            set
            {
                TheBotClient = value;
            }
        }


        public BotClient TheBotClient
        {
            get
            {
                if (_mClient == null)
                {
                    DLRConsole.DebugWriteLine("" + this + " has no TheBotClient?!");
                    return cogbot.Listeners.WorldObjects.GridMaster.client;
                }
                return _mClient;
            }
            set
            {
                _mClient = value;
            }
        }

        public static ClientManager ClientManager
        {
            get
            {
                return ClientManager.SingleInstance;
            }
        }

        public cogbot.Listeners.WorldObjects WorldSystem
        {
            get
            {
                if (_mClient == null) return cogbot.Listeners.WorldObjects.GridMaster;
                return _mClient.WorldSystem;
            }
        }

        public SimAvatarClient TheSimAvatar
        {
            get
            {
                _mClient = _mClient ?? Client;
                return _mClient.TheSimAvatar;
            }
        }

        public CmdResult acceptInputWrapper(string verb, string args,UUID callerID, OutputDelegate writeLine)
        {
            CallerID = callerID;
            success = failure = 0;
            this.WriteLine = writeLine;
            return acceptInput(verb, Parser.ParseArgs(args), writeLine);
        }

        public virtual CmdResult Execute(string[] args, UUID fromAgentID, OutputDelegate writeLine)
        {
            CallerID = fromAgentID;
            success = failure = 0;
            var wlpre = this.WriteLine;
            this.WriteLine = writeLine;
            Parser p = Parser.ParseArgs(String.Join(" ", args));
            p.tokens = args;
            try
            {
                return acceptInput(Name, p, writeLine);
            }
            finally
            {
                WriteLine = wlpre;
            }
        }

        public virtual CmdResult ExecuteCmd(string[] args, UUID fromAgentID, OutputDelegate writeLine)
        {
            CallerID = fromAgentID;
            success = failure = 0;
            this.WriteLine = writeLine;
            Parser p = Parser.ParseArgs(String.Join(" ", args));
            p.tokens = args;
            try
            {
                return acceptInput(Name, p, writeLine);
            }
            finally
            {
              //??  WriteLine = StaticWriteLine;
            }
        }


        public int CompareTo(object obj)
        {
            if (obj is Command)
            {
                Command c2 = (Command)obj;
                return Category.CompareTo(c2.Category);
            }
            else
                throw new ArgumentException("Object is not of type Command.");
        }

  
        // Helpers

        protected Vector3 GetSimPosition()
        {
            return TheSimAvatar.SimPosition;
        }

        public UUID UUIDParse(string p)
        {
            UUID uuid = UUID.Zero;
            int argsUsed;
            if (UUIDTryParse(new[] { p }, 0, out uuid,out argsUsed)) return uuid;
            return UUID.Parse(p);
        }

        public bool UUIDTryParse(string[] args, int start, out UUID target, out int argsUsed)
        {
            if (args==null|| args.Length==0)
            {
                target = UUID.Zero;
                argsUsed = 0;
                return false;
            }
            string p = args[0];
            if (p.Contains("-") && UUID.TryParse(p, out target))
            {
                argsUsed = 1;
                return true;
            }
            List<SimObject> OS = WorldSystem.GetPrimitives(args, out argsUsed);
            if (OS.Count == 1)
            {
                target = OS[0].ID;
                argsUsed = 1;
                return true;
            }
            target = WorldSystem.GetAssetUUID(p, AssetType.Unknown);
            if (target != UUID.Zero)
            {
                argsUsed = 1;
                return true;
            }
            argsUsed = 0;
            return false;
        }

        public CmdResult Failure(string message)
        {
            var Name = "Failure";
            if (!message.ToLower().Contains(Name.ToLower()))
            {
                message = Name + ": " + message;
            }
            failure++;
            return Result(message, false);
        }

        public CmdResult Success(string message)
        {
            var Name = "Success";
            if (!message.ToLower().Contains(Name.ToLower()))
            {
                message = Name + ": " + message;
            }
            success++;
            return Result(message, true);
        }

        public void LocalWL(string usage)
        {
            try
            {
                WriteLine(usage);
            }
            catch (Exception e)
            {

            }
            try
            {
                DLRConsole.DebugWriteLine(usage);
            }
            catch (Exception e)
            {
                DLRConsole.DebugWriteLine(e);
            }
        }

        protected CmdResult Result(string message, bool tf)
        {
            if (!message.ToLower().Contains(Name.ToLower()))
            {
                message = Name + ": " + message;
            }
            var cr = new CmdResult(message, tf, new OSDMap());
            LocalWL(cr.ToString());
            return cr;
        }
        protected CmdResult SuccessOrFailure()
        {
            var cr = new CmdResult(Name + " " + failure + " failures and " + success + " successes", failure == 0, new OSDMap());
            LocalWL(cr.ToString());
            return cr;
        }

        protected bool TryEnumParse(Type type, string[] names, int argStart, out int argsUsed, out object value)
        {
            ulong d = 0;
            argsUsed = 0;
            for (int i = argStart; i < names.Length; i++)
            {
                var name = names[i];

                Object e = null;
                try
                {
                    e = Enum.Parse(type, name);
                }
                catch (ArgumentException)
                {

                }
                if (e != null)
                {
                    d += (ulong) e.GetHashCode();
                    argsUsed++;
                    continue;
                }
                try
                {
                    e = Enum.Parse(type, name, true);
                }
                catch (ArgumentException)
                {

                }

                if (e != null)
                {
                    d += (ulong) e.GetHashCode();
                    argsUsed++;
                    continue;
                }
                ulong numd;
                if (ulong.TryParse(name, out numd))
                {
                    d += numd;
                    argsUsed++;
                    continue;
                }
                break;
            }
            if (argsUsed == 0)
            {
                value = null;
                return false;
            }
            Type etype = Enum.GetUnderlyingType(type);
            if (typeof (IConvertible).IsAssignableFrom(etype))
            {
                MethodInfo mi = etype.GetMethod("Parse",new Type[]{typeof(string)});
                value = mi.Invoke(null, new object[] {d.ToString()});
                return argsUsed > 0;
            }
            value = d;
            return argsUsed > 0;
        }

        static public bool IsEmpty(ICollection enumerable)
        {
            return enumerable == null || enumerable.Count == 0;
        }

        protected Simulator TryGetSim(string[] args, out int argsUsed)
        {
            if (args.Length > 0)
            {
                string s = String.Join(" ", args);
                SimRegion R = SimRegion.GetRegion(s, Client);
                if (R == null)
                {
                    argsUsed = 0;
                    WriteLine("cant find sim " + s);
                    return null;
                }

                Simulator sim = R.TheSimulator;
                if (sim == null) WriteLine("not connect to sim" + R);
                argsUsed = args.Length;
                return sim;
            }
            argsUsed = 0;
            return Client.Network.CurrentSim;
        }

    }
}
