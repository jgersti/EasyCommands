using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript {
    partial class Program {
        public static class Lexer {
            //Internal (Don't touch!)
            public static readonly Dictionary<string, List<IToken>> PropertyWords;

            static readonly string[] firstPassSeperators = new[] { "(", ")", "[", "]", ",", "*", "/", "!", "^", "..", "%", ">=", "<=", "==", "&&", "||", "@", "$", "->", "++", "+=", "--", "-=", "::"};
            static readonly string[] secondPassSeperators = new[] { "<", ">", "=", "&", "|", "-", "+", "?", ":" };
            static readonly string[] thirdPassSeperators = new[] { "." };

            static Lexer() {
                PropertyWords = new Dictionary<string, List<IToken>>();
                //Ignored words that have no command parameters
                AddWords(Words("the", "than", "turned", "block", "panel", "chamber", "drive", "to", "from", "then", "of", "either", "for", "in", "do", "does", "second", "seconds", "be", "being", "digits", "digit"), new IgnoreToken());

                //Selector Related Words
                AddWords(Words("blocks", "group", "panels", "chambers", "drives"), new GroupToken());
                AddWords(Words("my", "self", "this"), new Selftoken());
                AddWords(Words("$"), new VariableSelectorToken());

                //Direction Words
                AddDirectionWords(Words("up", "upward", "upwards", "upper"), Direction.UP);
                AddDirectionWords(Words("down", "downward", "downwards", "lower"), Direction.DOWN);
                AddDirectionWords(Words("left", "lefthand"), Direction.LEFT);
                AddDirectionWords(Words("right", "righthand"), Direction.RIGHT);
                AddDirectionWords(Words("forward", "forwards", "front"), Direction.FORWARD);
                AddDirectionWords(Words("backward", "backwards", "back"), Direction.BACKWARD);
                AddDirectionWords(Words("clockwise", "clock"), Direction.CLOCKWISE);
                AddDirectionWords(Words("counterclockwise", "counter", "counterclock"), Direction.COUNTERCLOCKWISE);

                //Action Words
                AddWords(Words("bind", "tie", "link"), new AssignmentToken(true));
                AddWords(Words("move", "go", "tell", "turn", "rotate", "set", "assign", "allocate", "designate", "apply"), new AssignmentToken());
                AddWords(Words("reverse", "reversed"), new ReverseToken());
                AddWords(Words("raise", "extend"), new AssignmentToken(), new DirectionToken(Direction.UP));
                AddWords(Words("retract"), new AssignmentToken(), new DirectionToken(Direction.DOWN));
                AddWords(Words("increase", "increment"), new IncreaseToken());
                AddWords(Words("decrease", "decrement", "reduce"), new IncreaseToken(false));
                AddWords(Words("++", "+="), new IncrementToken());
                AddWords(Words("--", "-="), new IncrementToken(false));
                AddWords(Words("global"), new GlobalToken());
                AddWords(Words("by"), new RelativeToken());

                //Value Words
                AddWords(Words("on", "begin", "true", "start", "started", "resume", "resumed"), new BooleanToken(true));
                AddWords(Words("off", "terminate", "cancel", "end", "false", "stopped", "halt", "halted"), new BooleanToken(false));

                //Property Words
                AddPropertyWords(AllWords(PluralWords("height", "length", "level", "size", "period", "scale")), Property.LEVEL);
                AddPropertyWords(PluralWords("angle"), Property.ANGLE);
                AddPropertyWords(AllWords(PluralWords("speed", "rate", "pace"), Words("velocity", "velocities")), Property.VELOCITY);
                AddPropertyWords(Words("connect", "attach", "connected", "attached", "dock", "docked", "docking"), Property.CONNECTED);
                AddPropertyWords(Words("disconnect", "detach", "disconnected", "detached", "undock", "undocked"), Property.CONNECTED, false);
                AddPropertyWords(Words("lock", "locked", "freeze", "frozen", "brake", "braking", "handbrake", "permanent"), Property.LOCKED);
                AddPropertyWords(Words("unlock", "unlocked", "unfreeze"), Property.LOCKED, false);
                AddPropertyWords(Words("run", "running", "execute", "executing", "script"), Property.RUN);
                AddPropertyWords(Words("use", "used", "occupy", "occupied", "control", "controlled"), Property.USE);
                AddPropertyWords(Words("unused", "unoccupied", "vacant", "available"), Property.USE, false);
                AddPropertyWords(Words("done", "ready", "complete", "finished", "built", "finish", "pressurized", "depressurized"), Property.COMPLETE);
                AddPropertyWords(Words("clear", "wipe", "erase"), Property.COMPLETE, false);
                AddPropertyWords(Words("open", "opened"), Property.OPEN);
                AddPropertyWords(Words("close", "closed", "shut"), Property.OPEN, false);
                AddPropertyWords(PluralWords("font"), Property.FONT);
                AddPropertyWords(PluralWords("text", "message", "argument"), Property.TEXT);
                AddPropertyWords(AllWords(Words("colors"), PluralWords("foreground")), Property.COLOR);
                AddAmbiguousWords(Words("color"), new PropertyToken(Property.COLOR));
                AddPropertyWords(PluralWords("background"), Property.BACKGROUND);
                AddPropertyWords(Words("power", "powered"), Property.POWER);
                AddPropertyWords(Words("enable", "enabled", "arm", "armed"), Property.ENABLE);
                AddPropertyWords(Words("disable", "disabled", "disarm", "disarmed"), Property.ENABLE, false);
                AddPropertyWords(Words("music", "sound", "song", "track", "image", "play", "playing", "unsilence"), Property.MEDIA);
                AddPropertyWords(Words("silence", "silent", "quiet"), Property.MEDIA, false);
                AddPropertyWords(Words("sounds", "songs", "images", "tracks"), Property.MEDIA_LIST);
                AddPropertyWords(AllWords(PluralWords("volume", "output"), Words("intensity", "intensities")), Property.VOLUME);
                AddPropertyWords(AllWords(PluralWords("range", "distance", "limit", "delay"), Words("radius", "radii", "capacity", "capacities")), Property.RANGE);
                AddPropertyWords(PluralWords("interval"), Property.INTERVAL);
                AddPropertyWords(PluralWords("offset", "padding"), Property.OFFSET);
                AddPropertyWords(PluralWords("falloff"), Property.FALLOFF);
                AddPropertyWords(Words("trigger", "triggered", "detect", "detected", "trip", "tripped", "deploy", "deployed", "shoot", "shooting", "shot", "detonate", "fire", "firing"), Property.TRIGGER);
                AddPropertyWords(Words("pressure", "pressurize", "pressurizing", "supply", "supplying", "generate", "generating", "discharge", "discharging", "broadcast", "broadcasting", "assemble", "assembling"), Property.SUPPLY);
                AddPropertyWords(Words("stockpile", "stockpiling", "depressurize", "depressurizing", "gather", "gathering", "intake", "recharge", "recharging", "consume", "consuming", "collect", "collecting", "disassemble", "disassembling"), Property.SUPPLY, false);
                AddPropertyWords(AllWords(PluralWords("ratio", "percentage", "percent", "completion"), Words("progress", "progresses")), Property.RATIO);
                AddPropertyWords(PluralWords("input", "pilot", "user"), Property.INPUT);
                AddPropertyWords(PluralWords("roll", "rollInput", "rotation"), Property.ROLL_INPUT);
                AddPropertyWords(Words("auto", "autopilot", "refill", "drain", "draining", "cooperate", "cooperating"), Property.AUTO);
                AddPropertyWords(AllWords(PluralWords("override", "dampener"), Words("overridden")), Property.OVERRIDE);
                AddPropertyWords(PluralWords("direction"), Property.DIRECTION);
                AddPropertyWords(PluralWords("position", "location", "alignment"), Property.POSITION);
                AddPropertyWords(Words("target", "destination", "waypoint", "coords", "coordinates"), Property.TARGET);
                AddPropertyWords(Words("waypoints", "destinations"), Property.WAYPOINTS);
                AddPropertyWords(Words("targetvelocity"), Property.TARGET_VELOCITY);
                AddPropertyWords(AllWords(PluralWords("strength", "force", "torque"), Words("gravity", "gravities")), Property.STRENGTH);
                AddPropertyWords(Words("naturalgravity", "naturalgravities", "planetgravity", "planetgravities"), Property.NATURAL_GRAVITY);
                AddPropertyWords(Words("artificialgravity", "artificialgravities"), Property.ARTIFICIAL_GRAVITY);
                AddPropertyWords(PluralWords("countdown"), Property.COUNTDOWN);
                AddPropertyWords(Words("name", "label"), Property.NAME);
                AddPropertyWords(Words("names", "labels"), Property.NAMES);
                AddPropertyWords(Words("show", "showing"), Property.SHOW);
                AddPropertyWords(Words("hide", "hiding"), Property.SHOW, false);
                AddPropertyWords(Words("properties", "attributes"), Property.PROPERTIES);
                AddPropertyWords(Words("actions"), Property.ACTIONS);
                AddPropertyWords(Words("types", "blueprints"), Property.TYPES);
                AddPropertyWords(PluralWords("altitude", "elevation"), Property.ALTITUDE);
                AddPropertyWords(Words("weight", "mass"), Property.WEIGHT);
                AddPropertyWords(Words("data", "customdata"), Property.DATA);
                AddPropertyWords(Words("info", "details", "detailedinfo"), Property.INFO);

                //ValueProperty Words
                AddWords(PluralWords("amount"), new ValuePropertyToken(ValueProperty.AMOUNT));
                AddWords(Words("property", "attribute"), new ValuePropertyToken(ValueProperty.PROPERTY));
                AddWords(Words("action"), new ValuePropertyToken(ValueProperty.ACTION));
                AddWords(Words("produce", "producing", "create", "creating", "build", "building", "make", "making"), new ValuePropertyToken(ValueProperty.CREATE));
                AddWords(Words("destroy", "destroying", "recycle", "recycling"), new ValuePropertyToken(ValueProperty.DESTROY));

                //Special Command Words
                AddWords(Words("times", "iterations"), new RepeatToken());
                AddWords(Words("wait", "hold"), new WaitToken());
                AddWords(Words("call", "gosub"), new FunctionToken(false));
                AddWords(Words("goto"), new FunctionToken(true));
                AddWords(Words("listen", "channel", "register", "subscribe"), new ListenToken(true));
                AddWords(Words("forget", "dismiss", "ignore", "deregister", "unsubscribe"), new ListenToken(false));
                AddWords(Words("send"), new SendToken());
                AddWords(Words("print", "log", "echo", "write"), new PrintToken());
                AddWords(Words("queue", "schedule"), new QueueToken(false));
                AddWords(Words("async", "parallel"), new QueueToken(true));
                AddWords(Words("transfer", "give"), new TransferToken(true));
                AddWords(Words("take"), new TransferToken(false));
                AddWords(Words("->"), new KeyedVariableToken());
                AddWords(Words("?"), new TernaryConditionIndicatorToken());
                AddWords(Words("::"), new TernaryConditionSeparatorToken());
                AddWords(Words(":"), new ColonSeparatorToken());
                AddWords(Words("each", "every"), new IteratorToken());

                //Conditional Words
                AddWords(Words("if"), new IfToken(false, false, false));
                AddWords(Words("unless"), new IfToken(true, false, false));
                AddWords(Words("while"), new IfToken(false, true, false));
                AddWords(Words("until"), new IfToken(true, true, false));
                AddWords(Words("when"), new IfToken(true, true, true));
                AddWords(Words("else", "otherwise"), new ElseToken());
                AddWords(Words("that", "which", "whose"), new ThatToken());

                //Comparison Words
                AddWords(Words("less", "<", "below"), new ComparisonToken((a, b) => a.CompareTo(b) < 0));
                AddWords(Words("<="), new ComparisonToken((a, b) => a.CompareTo(b) <= 0));
                AddWords(Words("is", "are", "equal", "equals", "=", "=="), new ComparisonToken((a, b) => a.CompareTo(b) == 0));
                AddWords(Words(">="), new ComparisonToken((a, b) => a.CompareTo(b) >= 0));
                AddWords(Words("greater", ">", "above", "more"), new ComparisonToken((a, b) => a.CompareTo(b) > 0));
                AddWords(Words("contain", "contains"), new ComparisonToken((a, b) => Operations.Evaluate(BinaryOperator.CONTAINS, a, b).AsBool()));

                //Aggregation Words
                AddWords(Words("any"), new AggregationModeToken(AggregationMode.ANY));
                AddWords(Words("all"), new AggregationModeToken(AggregationMode.ALL));
                AddWords(Words("none"), new AggregationModeToken(AggregationMode.NONE));
                AddWords(Words("average", "avg"), new PropertyAggregationToken((blocks, primitiveSupplier) => PROGRAM.SumAggregator(blocks, primitiveSupplier).Divide(Primitive.From(Math.Max(1, blocks.Count())))));
                AddWords(Words("minimum", "min"), new PropertyAggregationToken((blocks, primitiveSupplier) => blocks.Select(primitiveSupplier).Min() ?? Primitive.From(0)));
                AddWords(Words("maximum", "max"), new PropertyAggregationToken((blocks, primitiveSupplier) => blocks.Select(primitiveSupplier).Max() ?? Primitive.From(0)));
                AddWords(Words("count"), new PropertyAggregationToken((blocks, primitiveSupplier) => Primitive.From(blocks.Count())));
                AddAmbiguousWords(Words("number"), new PropertyAggregationToken((blocks, primitiveSupplier) => Primitive.From(blocks.Count())));
                AddWords(Words("sum", "total"), new PropertyAggregationToken(PROGRAM.SumAggregator));
                AddWords(Words("collection"), new PropertyAggregationToken((blocks, primitiveSupplier) => Primitive.From(NewKeyedList(blocks.Select(b => new StaticVariable(primitiveSupplier(b)))))));
                AddAmbiguousWords(Words("list"), new PropertyAggregationToken((blocks, primitiveSupplier) => Primitive.From(NewKeyedList(blocks.Select(b => new StaticVariable(primitiveSupplier(b)))))));

                //Operations Words
                AddWords(Words("("), new OpenParenthesisToken());
                AddWords(Words(")"), new CloseParenthesisToken());
                AddWords(Words("and", "&", "&&", "but", "yet"), new AndToken());
                AddWords(Words("or", "|", "||"), new OrToken());
                AddWords(Words("not", "!", "stop"), new NotToken());
                AddWords(Words("@"), new IndexToken());

                AddRightUnaryOperationWords(Words("absolute", "abs"), UnaryOperator.ABS);
                AddRightUnaryOperationWords(Words("sqrt"), UnaryOperator.SQRT);
                AddRightUnaryOperationWords(Words("sin"), UnaryOperator.SIN);
                AddRightUnaryOperationWords(Words("cosine", "cos"), UnaryOperator.COS);
                AddRightUnaryOperationWords(Words("tangent", "tan"), UnaryOperator.TAN);
                AddRightUnaryOperationWords(Words("arcsin", "asin"), UnaryOperator.ASIN);
                AddRightUnaryOperationWords(Words("arccos", "acos"), UnaryOperator.ACOS);
                AddRightUnaryOperationWords(Words("arctan", "atan"), UnaryOperator.ATAN);
                AddRightUnaryOperationWords(Words("sort", "sorted"), UnaryOperator.SORT);
                AddRightUnaryOperationWords(Words("ln"), UnaryOperator.LN);
                AddRightUnaryOperationWords(Words("rand", "random", "randomize"), UnaryOperator.RANDOM);
                AddRightUnaryOperationWords(Words("shuffle", "shuffled"), UnaryOperator.SHUFFLE);
                AddRightUnaryOperationWords(Words("sign", "quantize"), UnaryOperator.SIGN);

                AddLeftUnaryOperationWords(Words("tick", "ticks"), UnaryOperator.TICKS);
                AddLeftUnaryOperationWords(Words("keys", "indexes"), UnaryOperator.KEYS);
                AddLeftUnaryOperationWords(Words("values"), UnaryOperator.VALUES);
                AddLeftUnaryOperationWords(Words("type"), UnaryOperator.TYPE);

                //Tier 0 Operations
                AddWords(Words("dot", "."), new BinaryOperandToken(BinaryOperator.DOT, 0));

                //Tier 1 Operations
                AddBinaryOperationWords(Words("pow", "^", "xor"), BinaryOperator.EXPONENT, 1);

                //Tier 2 Operations
                AddBinaryOperationWords(Words("multiply", "*"), BinaryOperator.MULTIPLY, 2);
                AddBinaryOperationWords(Words("divide", "/"), BinaryOperator.DIVIDE, 2);
                AddBinaryOperationWords(Words("mod", "%"), BinaryOperator.MOD, 2);
                AddBinaryOperationWords(Words("split", "separate", "separated"), BinaryOperator.SPLIT, 2);
                AddBinaryOperationWords(Words("join", "joined"), BinaryOperator.JOIN, 2);

                //Tier 3 Operations
                AddBinaryOperationWords(Words("plus", "+"), BinaryOperator.ADD, 3);
                AddBinaryOperationWords(Words("minus"), BinaryOperator.SUBTRACT, 3);

                //Tier 4 Operations
                AddBinaryOperationWords(Words(".."), BinaryOperator.RANGE, 4);

                AddWords(Words("-"), new MinusToken());
                AddWords(Words("round", "rnd", "rounded"), new RoundToken());
                AddWords(Words("as", "cast", "resolve", "resolved"), new CastToken());

                //List Words
                AddWords(Words("["), new OpenBracketToken());
                AddWords(Words("]"), new CloseBracketToken());
                AddWords(Words(","), new ListSeparatorToken());

                //Control Types
                AddControlWords(Words("restart", "reset", "reboot"), thread => {
                    PROGRAM.ClearAllState();
                    throw new InterruptException(ProgramState.RUNNING);
                });
                AddControlWords(Words("repeat", "loop", "rerun", "replay"), thread => {
                    thread.Command = thread.Command.Clone();
                    return false;
                });
                AddControlWords(Words("exit"), thread => {
                    PROGRAM.ClearAllState();
                    throw new InterruptException(ProgramState.STOPPED);
                });
                AddControlWords(Words("pause"), thread => {
                    if (PROGRAM.state != ProgramState.PAUSED) throw new InterruptException(ProgramState.PAUSED);
                    return true;
                });
                AddControlWords(Words("break"), thread => {
                    GetInterrupableCommand(thread, "break").Break();
                    return false;
                });
                AddControlWords(Words("continue"), thread => {
                    GetInterrupableCommand(thread, "continue").Continue();
                    return false;
                });
                AddControlWords(Words("return"), thread => {
                    FunctionCommand currentFunction = thread.GetCurrentCommand<FunctionCommand>(command => true);
                    if (currentFunction == null) throw new Exception("Invalid use of return command");
                    currentFunction.function = new NullCommand();
                    return false;
                });

                //Blocks
                AddBlockWords(Words("piston"), Block.PISTON);
                AddBlockWords(Words("light", "spotlight"), Block.LIGHT);
                AddBlockWords(Words("rotor"), Block.ROTOR);
                AddBlockWords(Words("hinge"), Block.HINGE);
                AddBlockWords(Words("program", "programmable"), Block.PROGRAM);
                AddBlockWords(Words("timer"), Block.TIMER);
                AddBlockWords(Words("projector"), Block.PROJECTOR);
                AddBlockWords(Words("merge"), Words(), Block.MERGE);
                AddBlockWords(Words("connector"), Block.CONNECTOR);
                AddBlockWords(Words("welder"), Block.WELDER);
                AddBlockWords(Words("grinder"), Block.GRINDER);
                AddBlockWords(Words("door", "hangar", "bay", "gate"), Block.DOOR);
                AddBlockWords(PluralWords("display", "screen", "lcd"), Words(), Block.DISPLAY);
                AddBlockWords(Words("speaker", "alarm", "siren"), Block.SOUND);
                AddBlockWords(Words("camera"), Block.CAMERA);
                AddBlockWords(Words("sensor"), Block.SENSOR);
                AddBlockWords(Words("beacon"), Block.BEACON);
                AddBlockWords(Words("antenna"), Block.ANTENNA);
                AddBlockWords(Words("ship", "rover", "cockpit", "seat", "station"), Block.COCKPIT);
                AddBlockWords(Words("cryo"), Block.CRYO_CHAMBER);
                AddBlockWords(Words("drone", "remote", "robot"), Block.REMOTE);
                AddBlockWords(Words("thruster"), Block.THRUSTER);
                AddBlockWords(Words("airvent", "vent"), Block.AIRVENT);
                AddBlockWords(Words("gun", "railgun", "cannon", "autocannon", "rocket", "missile", "launcher"), Block.GUN);
                AddBlockWords(Words("turret"), Block.TURRET);
                AddBlockWords(Words("generator"), Block.GENERATOR);
                AddBlockWords(Words("tank"), Block.TANK);
                AddBlockWords(Words("magnet", "gear"), Block.MAGNET);
                AddBlockWords(Words("battery"), Words("batteries"), Block.BATTERY);
                AddBlockWords(Words("chute", "parachute"), Block.PARACHUTE);
                AddBlockWords(Words("wheel"), Words("wheels", "suspension"), Block.SUSPENSION);
                AddBlockWords(Words("detector"), Block.DETECTOR);
                AddBlockWords(Words("drill"), Block.DRILL);
                AddBlockWords(Words("engine"), Block.ENGINE);
                AddBlockWords(Words("turbine"), Block.TURBINE);
                AddBlockWords(Words("reactor"), Block.REACTOR);
                AddBlockWords(Words("solar"), Block.SOLAR_PANEL);
                AddBlockWords(Words("sorter"), Block.SORTER);
                AddBlockWords(Words("gyro", "gyroscope"), Block.GYROSCOPE);
                AddBlockWords(Words("gravitygenerator"), Block.GRAVITY_GENERATOR);
                AddBlockWords(Words("gravitysphere"), Block.GRAVITY_SPHERE);
                AddBlockWords(Words("cargo", "container", "inventory", "inventories"), Words("cargos", "containers"), Block.CARGO);
                AddBlockWords(Words("warhead", "bomb"), Block.WARHEAD);
                AddBlockWords(Words("assembler"), Block.ASSEMBLER);
                AddBlockWords(Words("collector"), Block.COLLECTOR);
                AddBlockWords(Words("ejector"), Block.EJECTOR);
                AddBlockWords(Words("decoy"), Block.DECOY);
                AddBlockWords(Words("jump", "jumpdrive"), Block.JUMPDRIVE);
                AddBlockWords(Words("laser", "laserantenna"), Block.LASER_ANTENNA);
                AddBlockWords(Words("terminal"), Block.TERMINAL);
                AddBlockWords(Words("refinery"), Words("refineries"), Block.REFINERY);
                AddBlockWords(Words("heatvent"), Block.HEAT_VENT);
            }

            static void AddControlWords(IEnumerable<string> words, ControlFunction function) =>
                AddWords(words, new CommandToken(new ControlCommand { controlFunction = function }));
            static void AddDirectionWords(IEnumerable<string> words, Direction dir) =>
                AddWords(words, new DirectionToken(dir));

            static void AddPropertyWords(IEnumerable<string> words, Property property, bool nonNegative = true) {
                if (!nonNegative) AddWords(words, new PropertyToken(property), new BooleanToken(false));
                else AddWords(words, new PropertyToken(property));
            }

            static void AddRightUnaryOperationWords(IEnumerable<string> words, UnaryOperator op) {
                AddWords(words, new UnaryOperationToken(op));
                UnaryOperatorToString[op] = words.First();
            }

            static void AddLeftUnaryOperationWords(IEnumerable<string> words, UnaryOperator op) {
                AddWords(words, new LeftUnaryOperationToken(op));
                UnaryOperatorToString[op] = words.First();
            }

            static void AddBinaryOperationWords(IEnumerable<string> words, BinaryOperator op, int tier) {
                AddWords(words, new BinaryOperandToken(op, tier));
                BinaryOperatorToString[op] = words.First();
            }

            //Assume group words are just blockWords with "s" added to the end
            static void AddBlockWords(IEnumerable<string> blockWords, Block blockType) => AddBlockWords(blockWords, blockWords.Select(b => b + "s"), blockType);

            static void AddBlockWords(IEnumerable<string> blockWords, IEnumerable<string> groupWords, Block blockType) {
                AddWords(blockWords, new BlockTypeToken(blockType));
                AddWords(groupWords, new BlockTypeToken(blockType), new GroupToken());
            }

            static void AddAmbiguousWords(IEnumerable<string> words, params IToken[] commandParameters) {
                foreach (var word in words)
                    AddWords(Words(word), new AmbiguousStringToken(word, false, new AmbiguousToken(commandParameters)));
            }

            static void AddWords(IEnumerable<string> words, params IToken[] commandParameters) {
                foreach (var word in words)
                    PropertyWords.Add(word, commandParameters.ToList());
            }

            public static List<IToken> GetTokens(string input) => Tokenize(input).SelectMany(Match).ToList();

            public static List<IToken> Match(Lexeme lexeme) {
                var tokens = NewList<IToken>();
                if (lexeme.isExplicit)
                    tokens.Add(new VariableToken(GetStaticVariable(lexeme.original)));
                else if (lexeme.isString)
                    tokens.Add(new AmbiguousStringToken(lexeme.original, false, GetTokens(lexeme.lexeme).ToArray()));
                else if (PropertyWords.ContainsKey(lexeme.lexeme))
                    tokens.AddList(PropertyWords[lexeme.lexeme]);
                else //If no property matches, must be a string
                    tokens.Add(new AmbiguousStringToken(lexeme.original, true));

                tokens[0].Lexeme = lexeme.original;
                return tokens;
            }

            delegate IEnumerable<string> Pass(string s);
            public static IEnumerable<Lexeme> Tokenize(string commandString) {
                Pass thirdPass = v => SeperatorPass(v, thirdPassSeperators);
                Pass secondPass = v => SeperatorPass(v, secondPassSeperators, w => PrimitivePass(w, thirdPass));
                Pass firstPass = v => SeperatorPass(v, firstPassSeperators, w => PrimitivePass(w, secondPass));

                return (string.IsNullOrWhiteSpace(commandString) || commandString.Trim().StartsWith("#"))
                ? NewList<Lexeme>()
                : TokenizeEnclosed(commandString, "`\'\"",
                    u => u.Replace(" : ", " :: ")
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .SelectMany(v => firstPass(v))
                        .Select(v => new Lexeme(v, false, false)));
            }

            static IEnumerable<Lexeme> TokenizeEnclosed(string token, string characters, Func<string, IEnumerable<Lexeme>> parseSubTokens) =>
                characters.Length == 0 ? parseSubTokens(token) :
                    token.Trim().Split(characters[0])
                    .SelectMany((element, index) => index % 2 == 0  // If even index
                        ? TokenizeEnclosed(element, characters.Remove(0, 1), parseSubTokens)  // Split the item
                        : Once(new Lexeme(element, true, characters.Length > 1)));  // Keep the entire item

            static IEnumerable<string> PrimitivePass(string token, Pass nextPass = null) {
                Primitive ignored;
                return Primitive.TryParse(token, out ignored) || nextPass == null ? Once(token) : nextPass(token);
            }
            static IEnumerable<string> SeperatorPass(string command, string[] separators, Pass nextPass = null) {
                var newCommand = command;
                foreach (var s in separators) newCommand = newCommand.Replace(s, " " + s + " ");
                return newCommand
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(token => separators.Contains(token) || nextPass == null ? Once(token) : nextPass(token));
            }

            public class Lexeme {
                public string lexeme, original;
                public bool isString, isExplicit;

                public Lexeme(string lexeme, bool isString, bool isExplicit) {
                    this.isString = isString;
                    this.isExplicit = isExplicit;
                    this.lexeme = lexeme.ToLower();
                    original = lexeme;
                }

                public override string ToString() => lexeme;
            }
        }
    }
}
