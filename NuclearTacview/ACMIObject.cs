using System;
using System.Collections.Generic;

namespace NuclearTacview
{
    abstract public class ACMIObject(int id)
    {
        public readonly int id = id;

        public event Action<string, int[], string>? OnEvent;

        public abstract Dictionary<string, string> Init();

        public abstract Dictionary<string, string> Update();

        protected void FireEvent(string key, int[] ids, string text)
        {
            OnEvent?.Invoke(key, ids, text);
        }
    }
}
