using System;
using System.Collections.Generic;

namespace NuclearTacview
{
    abstract public class ACMIObject(long id)
    {
        public readonly long id = id;

        public event Action<string, long[], string>? OnEvent;

        public abstract Dictionary<string, string> Init();

        public abstract Dictionary<string, string> Update();

        protected void FireEvent(string key, long[] ids, string text)
        {
            OnEvent?.Invoke(key, ids, text);
        }
    }
}
