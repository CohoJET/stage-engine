using System;
using UnityEngine;

namespace StageEngine.Core.Utility
{
    public class SignedLogger
    {
        public string Prefix { get; }

        private SignedLogger(string prefix)
        {
            Prefix = prefix;
        }
        public static SignedLogger GetInstance(Type type)
        {
            return new SignedLogger($"[{type.Name}]");
        }

        public void Log(string message)
        {
            Debug.Log($"{Prefix} {message}");
        }
        public void LogWarning(string message)
        {
            Debug.LogWarning($"{Prefix} {message}");
        }
        public void LogError(string message)
        {
            Debug.LogError($"{Prefix} {message}");
        }
    }
}
