using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Web;
using System.Net;

namespace SearchNow {
    class SearchEngine{
        public string Name;
        public string Shortcut;
        public string Query;
    }

    class SearchEngines {
        private List<SearchEngine> engines_collection;
        private Dictionary<string, string> name_from_shortcut, query_from_name;
        private string selected_engine;


        public SearchEngines(string file) {
            ParseEngines(file);
            LoadDefault();
        }

        private void ParseEngines(string file) {
            engines_collection = new List<SearchEngine>();

            //TODO: try
            XDocument xml = XDocument.Load(file);
            var query =
                from engine in xml.Descendants("Engine")
                select new SearchEngine{
                    Name = engine.Element("Name").Value,
                    Shortcut = engine.Element("Shortcut").Value,
                    Query = engine.Element("Query").Value
                };
            engines_collection = query.ToList();


            name_from_shortcut = new Dictionary<string, string>();
            query_from_name = new Dictionary<string, string>();

            foreach (SearchEngine engine in engines_collection) {
                query_from_name.Add(engine.Name, engine.Query);
                name_from_shortcut.Add(engine.Shortcut, engine.Name);
            }

        }

        private string CheckQueryForCommand(string query) {
            Match match = Regex.Match(query, @"\?c=([a-z A-Z]);");
            if (match.Success) {
                string match_text = match.Groups[0].Value;
                string command = match.Groups[1].Value;

                query = query.Replace(match_text, "");
                if (command.Equals("x")) {
                    Application.Current.Shutdown();
                }

                
            }
            return query;
        }

        private string CheckQueryForConfig(string query) {
            Match match = Regex.Match(query, @"\?l=([^<>:"" /\\\|\?\*]+.xml);");
            if (match.Success) {
                //reload was requested
                string text = match.Groups[0].Value;
                string file = match.Groups[1].Value;
                
                ParseEngines(file);
                LoadDefault();
                query = query.Replace(text, "");
            }
            /*
            match = Regex.Match(query, @"\?v=([0-9]{1,3});");
            if (match.Success) {
                //reload was requested
                string text = match.Groups[0].Value;
                int volume = Convert.ToInt32(match.Groups[1].Value);

                Audio.Volume = volume;
                query = query.Replace(text, "");
            }
            */

            match = Regex.Match(query, @"\?d=([a-z A-Z]+)\;");
            if (match.Success) {
                string match_text = match.Groups[0].Value;
                string shortcut = match.Groups[1].Value;
                if (name_from_shortcut.ContainsKey(shortcut)) {
                    //Shortcut is assigned to an engine
                    selected_engine = name_from_shortcut[shortcut];
                } else {
                    //Invalid shortcut!!
                    MessageBox.Show("invalid shortcut!");
                }
                return query.Replace(match_text, "");
            }
            //No valid config subquery
            return query;
        }


        private string ParseQuery(string query) {
            if (String.IsNullOrEmpty(query))
                return query;
            Match match = Regex.Match(query, @"\?e=([a-z A-Z]+):([\s\S]*)");
            if (match.Success) {
                //e is specified
                string shortcut = match.Groups[1].Value;
                string text = match.Groups[2].Value;
                if (name_from_shortcut.ContainsKey(shortcut)) {
                    //Engine with this shortcut exists
                    DoSearch(name_from_shortcut[shortcut], text);
                } else {
                    //Wrong engine shortcut
                    MessageBox.Show("Specified engine not found: " + shortcut);

                    DoSearch(selected_engine, text);
                    return query;
                }
            } else {
                //Normal input
                DoSearch(selected_engine, query);
            }
            return query;
        }

        private void DoSearch(string engine, string query) {
            string engine_query = query_from_name[engine];
            string uri;
            if (engine_query.StartsWith("http")) {
                uri = string.Format(engine_query, HttpUtility.UrlEncode(query));

                //Check if link is valid
                WebRequest request = WebRequest.Create(uri);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                response.Close();
                if (!(response == null || response.StatusCode != HttpStatusCode.OK)) {
                    System.Diagnostics.Process.Start(uri);
                } else {
                    MessageBox.Show("Connection error, or invalid search engine description!");
                }
            } else {
                uri= string.Format(engine_query, query);
                System.Diagnostics.Process.Start(uri);
            }
           
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
        /// Performs a search based on given query.
        /// </summary>
        /// <param name="query"></param>
        public string Search(string query) {
            return ParseQuery(CheckQueryForConfig(CheckQueryForCommand(query)));
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
}
