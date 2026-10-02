using System;
using System.Text;
using Multiplayer;
using Steamworks.NET;
using UnityEngine;

namespace Manager
{
    public class CommandLineManager : MonoBehaviour
    {
        //Command line prefix
        private const string HasArgPrefix = "+", HasNoArgPrefix = "-";

        private void Start()
        {
            if (!SteamManager.Initialized) return;
            
            Invoke(nameof(CheckCommandLineArgs),2f);
        }

        void CheckCommandLineArgs()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(arguments[i]))
                    continue;

                string key = arguments[i].Trim().ToLower();

                StringBuilder sb = new StringBuilder();

                if (key.StartsWith(HasArgPrefix))
                {
                    key = key.Substring(1, key.Length - 1);
                    
                    //Get the arg
                    for (int j = i+1; j < arguments.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(arguments[j]))
                            continue;

                        string value = arguments[j].Trim();

                        if (value.StartsWith(HasArgPrefix) || value.StartsWith(HasNoArgPrefix))
                            break;

                        sb.Append(value);
                        sb.Append(" ");
                    }
                    
                    ParseArgument(key,sb.Length==0 ? String.Empty : sb.ToString().Trim());
                }
            }
        }

        void ParseArgument(string key, string value)
        {
            if (String.CompareOrdinal(key, "connect") == 0)
            {
                ServerManager.Instance.Connect(value);
            }
        }
    }
}