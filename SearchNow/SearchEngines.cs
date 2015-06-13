using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using System.Diagnostics;
using System.Threading.Tasks;

using System.Windows;
using System.IO;

using System.Web;
using System.Net.NetworkInformation;


namespace SearchNow {
    class SearchEngine{
        public string Name;
        public string Shortcut;
        public string Query;
    }

    public delegate void MessageHandler(object sender, MessageEventArgs e);

    class SearchEngines {
        private List<SearchEngine> engines_collection;
        private Dictionary<string, string> name_from_shortcut, query_from_name;
        private string selected_engine, loaded_file;
        private bool definitions_loaded = false;

        public event MessageHandler MessageRecieved;

        protected virtual void OnMessage(MessageEventArgs e) {
            if (MessageRecieved != null) {
                MessageRecieved(this, e);
            }
        }
     
        /// <summary>
        /// Initialize with the default file
        /// </summary>
        public SearchEngines() {
            string default_file = (string)Properties.Settings.Default["default_definitions_file"];
            if (!File.Exists(default_file)) {
                File.WriteAllText(default_file, Properties.Resources.engines); //extract the default file.
                InformUser("Default file extracted.", MessageType.Info);
            }
            LoadDefinitionsFile(default_file);
        }

        #region EngineDefinitions

        private void LoadDefinitionsFile(string file) {
            ParseEngineDefinitions(file);
            if (definitions_loaded) {
                LoadDefault();
                loaded_file = file;
            }
        }

        private void ParseEngineDefinitions(string file) {
            engines_collection = new List<SearchEngine>();

            try {
                XDocument xml = XDocument.Load(file);
                var query =
                    from engine in xml.Descendants("Engine")
                    select new SearchEngine {
                        Name = engine.Element("Name").Value,
                        Shortcut = engine.Element("Shortcut").Value,
                        Query = engine.Element("Query").Value
                    };
                engines_collection = query.ToList();
            } catch (Exception ex) {
                engines_collection = new List<SearchEngine>();
                InformUser("Couldn't load definitions file. " + ex.Message, MessageType.Error);
            }

            name_from_shortcut = new Dictionary<string, string>();
            query_from_name = new Dictionary<string, string>();

            if(engines_collection.Count == 0) {
                //Nothing was loaded.
                definitions_loaded = false;
                return;
            }

            foreach (SearchEngine engine in engines_collection) {
                query_from_name.Add(engine.Name, engine.Query);
                name_from_shortcut.Add(engine.Shortcut, engine.Name);
            }
            definitions_loaded = true;
            InformUser(String.Format("Loaded {0} engine definitions!", engines_collection.Count), MessageType.Info);
        }
        #endregion
      

        #region Query
        private string CheckQueryForCommand(string query) {
            Match match = Regex.Match(query, @"\?cmd=([a-z A-Z]+);");
            if (match.Success) {
                string match_text = match.Groups[0].Value;
                string command = match.Groups[1].Value;

                query = CheckQueryForCommand(query.Replace(match_text, ""));
                switch (command) {
                    case "exit":
                        Action exit = () => {
                            Application.Current.Shutdown();
                        };
                        exit.DoAfter(TimeSpan.FromMilliseconds(500));
                        return String.Empty;
                    case "reload":
                        if (!String.IsNullOrEmpty(loaded_file)) {
                            //We have a stored loaded file. Try to reload
                            LoadDefinitionsFile(loaded_file);
                        } else {
                            //No file to reload
                            InformUser("No file loaded. Cannot reload.", MessageType.Error);
                        }
                        break;
                    case "edit":
                        if (File.Exists(loaded_file)) {
                            Process.Start(loaded_file);
                            InformUser("You should probably issue a reload command when you are done editing.", MessageType.Warn);
                        }
                        break;
                    case "showlog":
                        SendCommand(MessageCommand.ShowLog);
                        break;
                    default:
                        InformUser(String.Format("Bad command: [{0}]", command), MessageType.Error);
                        break;
                }       
            }
            return query;
        }

        private string CheckQueryForConfig(string query) {
            Match match = Regex.Match(query, @"\?cfg:(\w+)=([^;]+);");
            if (match.Success) {
                //config given
                string text = match.Groups[0].Value;
                string config = match.Groups[1].Value;
                string arg = match.Groups[2].Value;

                query = CheckQueryForConfig(query.Replace(text, "")); //Remove our match and check for more
                switch (config) {
                    case "default":
                        if (name_from_shortcut.ContainsKey(arg)) {
                            //Shortcut is assigned to an engine
                            SetDefault(name_from_shortcut[arg]);
                            InformUser(String.Format("Default engine changed to: [{0}]", name_from_shortcut[arg]), MessageType.Info);
                        } else if (query_from_name.ContainsKey(arg)) {
                            //Whole name given
                            SetDefault(arg);
                            InformUser(String.Format("Default engine changed to: [{0}]", arg), MessageType.Info);
                        } else {
                            //Invalid shortcut!!
                            InformUser(String.Format("Invalid argument [{0}]. Cannot set default engine.", arg), MessageType.Error);
                        }
                        break;
                    case "definitions":
                        if (Regex.IsMatch(arg, @"[^<>:"" /\\\|\?\*]+\.xml$")) {
                            LoadDefinitionsFile(arg);
                        } else {
                            InformUser(String.Format("Bad argument [{0}]. Was expecting an .xml file!", arg), MessageType.Error);
                        }
                        break;
                }             
            }
            match = Regex.Match(query, @"\?pcfg:(\w+);");
            if (match.Success) {
                string text = match.Groups[0].Value;
                string config = match.Groups[1].Value;
                string info;

                query = CheckQueryForConfig(query.Replace(text, ""));
                switch (config) {
                    case "default":
                        info = String.Format("Default engine is: [{0}]", selected_engine);
                        break;
                    case "definitions":
                        info = String.Format("Currently loaded definitions file is: [{0}]", loaded_file);
                        break;
                    default:
                        InformUser(String.Format("Bad config option: [{0}]", config), MessageType.Error);
                        return query;
                }

                InformUser(info, MessageType.Info);
            }
            return query;
        }


        private string ParseQuery(string query) {
            if (String.IsNullOrEmpty(query))
                return query; //Nothing to do here
            Match match = Regex.Match(query, @"\?e =([a-z A-Z]+):([\s\S]*);");
            if (match.Success) {
                //Specific engine requested
                string match_text = match.Groups[0].Value;
                string shortcut = match.Groups[1].Value;
                string text = match.Groups[2].Value;

                query = ParseQuery(query.Replace(match_text, ""));
                if (name_from_shortcut.ContainsKey(shortcut)) {
                    //Engine with this shortcut exists
                    DoSearch(name_from_shortcut[shortcut], text);
                } else {
                    //Wrong engine shortcut
                    InformUser(String.Format("Specified engine [{0}] not found!" , shortcut), MessageType.Error);
                    //DoSearch(selected_engine, text);
                }
            } else {
                //Normal input
                DoSearch(selected_engine, query);
                query = String.Empty;
            }
            return query;
        }
        #endregion

        private void DoSearch(string engine, string query) {
            string engine_query = query_from_name[engine];
            string uri;

            Uri uri_result;
            bool is_online = Uri.TryCreate(engine_query, UriKind.Absolute, out uri_result) &&
                                    (uri_result.Scheme == Uri.UriSchemeHttp || uri_result.Scheme == Uri.UriSchemeHttps);
            if (is_online) {
                //This is an online query
                uri = string.Format(engine_query, HttpUtility.UrlEncode(query));

                try {
                    //Check if link is valid
                    Ping ping = new Ping();
                    PingReply result = ping.Send(uri_result.Host); //This throws an exception if no internet
                    if (result.Status != IPStatus.Success) {
                        InformUser("Connection error, or invalid search engine definitions!", MessageType.Error);
                    } else {
                        Process.Start(uri);
                    }
                } catch (Exception) {
                    InformUser("Connection error. You probably can't connect to the internet.", MessageType.Error);
                }
            } else {
                uri= string.Format(engine_query, query);
                Process.Start(uri);
            }         
        }

        private void InformUser(string info, MessageType type) {
            OnMessage(new MessageEventArgs(new InformationMessage(info, type)));
        }

        private void SendCommand(MessageCommand command) {
            OnMessage(new MessageEventArgs(new InformationMessage(command)));
        }

        private void LoadDefault() {
            string loaded_engine = (string)Properties.Settings.Default["default_engine"];
            if (!query_from_name.ContainsKey(loaded_engine)) { 
                //Loaded engine is  NOT available
                //Set the first available engine as selected
                selected_engine = query_from_name[query_from_name.Keys.First()];

                //Make this engine default
                SetDefault(selected_engine);
            } else { //Loaded engine is available
                selected_engine = loaded_engine;
            }
        }


        /// <summary>
        /// Performs a search based on given query. Returns the new query string
        /// with modifications (if needed).
        /// </summary>
        /// <param name="query"></param>
        public void Search(string query) {
            string for_search = CheckQueryForConfig(CheckQueryForCommand(query));
            string after_search = ParseQuery(for_search);
            if ((after_search == for_search) && (query != for_search)) {
                //we had config/command but not any search
                SendCommand(MessageCommand.DontHide);
            }
        }

        public bool SetDefault(string engine) {
            if (query_from_name.ContainsKey(engine)) {
                //Engine exists
                Properties.Settings.Default["default_engine"] = selected_engine = engine;
                Properties.Settings.Default.Save();

                return true;
            } else {
                //Engine doesn't exist
                return false;
            }
        }

        public List<string> GetEngines() {
            return query_from_name.Keys.ToList();
        }

    }

    public enum MessageType {
        Error,
        Warn,
        Info
    }

    public enum MessageCommand {
        ShowLog,
        DontHide
    }

    public class InformationMessage {
        public string Text { get; private set; }
        public MessageType Type { get; private set; }

        public bool IsCommand { get; private set; }
        public MessageCommand Command { get; private set; }

        public InformationMessage(string Text, MessageType Type) {
            this.Text = Text;
            this.Type = Type;
            this.IsCommand = false;
        }

        public InformationMessage(MessageCommand Command) {
            this.IsCommand = true;
            this.Command = Command;
        }
    }

    public class MessageEventArgs : EventArgs {
        public InformationMessage Message { get; private set; }
        public MessageEventArgs(InformationMessage Message) {
            this.Message = Message;
        }
    }
}
