using System;
using System.Collections.Generic;
using UnityEngine;

namespace Console
{
    public abstract class ConsoleCommand : ScriptableObject,IConsoleCommand
    {
        [SerializeField] string commandWord = String.Empty;
        [SerializeField] string description = String.Empty;
        [SerializeField] string argsHint = String.Empty;
        public string CommandWord => commandWord;
        public string Description => description;
        public string ArgsHint => argsHint;
        public abstract bool Process(string[] args);

        public List<string> key = new() { "normal" };
        public List<Argument> args = new List<Argument>();

        [Serializable]
        public class Argument
        {
            public List<string> args = new List<string>();
        }
    }
}

