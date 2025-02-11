﻿using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NuclearTacview
{
    internal class ACMIWriter
    {
        private readonly StreamWriter output;
        private readonly DateTime reference;
        private TimeSpan lastUpdate;
        internal ACMIWriter(DateTime reference)
        {
            string dir = Application.persistentDataPath + "/Replays/";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string basename = dir + DateTime.Now.ToString("s").Replace(":", "-");
            string filename = basename + ".acmi";
            int postfix = 0;
            while (File.Exists(filename))
                filename = basename + $" ({++postfix}).acmi";

            output = File.CreateText(filename);
            this.reference = reference;

            output.WriteLine("FileType=text/acmi/tacview");
            output.WriteLine("FileVersion=2.2");

            Dictionary<string, string> initProps = new()
            {
                { "ReferenceTime", reference.ToString("s") + "Z" },
                { "DataSource", $"Nuclear Option {Application.version}" },
                { "DataRecorder", $"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION}" },
                { "Author", GameManager.LocalPlayer.PlayerName.Replace(",", "\\,") },
                { "RecordingTime", DateTime.Today.ToString("s") + "Z" },
            };

            Mission mission = MissionManager.CurrentMission;
            initProps.Add("Title", mission.Name.Replace(",", "\\,"));

            string briefing = mission.missionSettings.description.Replace(",", "\\,");
            if (briefing != "")
                initProps.Add("Briefing", briefing);

            output.WriteLine($"0,{StringifyProps(initProps)}");
            output.Flush();
        }

        ~ACMIWriter()
        {
            output.Close();
        }

        internal void UpdateObject(ACMIObject aObject, DateTime updateTime, Dictionary<string, string> props)
        {
            if (props.Count == 0)
                return;

            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                output.WriteLine("#" + diff.TotalSeconds);
            }

            output.WriteLine($"{aObject.id:X},{StringifyProps(props)}");
        }

        internal void RemoveObject(ACMIObject aObject, DateTime updateTime)
        {
            TimeSpan diff = updateTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                output.WriteLine("#" + diff.TotalSeconds);
            }

            output.WriteLine($"-{aObject.id:X}");
        }

        internal void WriteEvent(DateTime eventTime, string name, string[] items)
        {
            TimeSpan diff = eventTime - reference;
            if (diff != lastUpdate)
            {
                lastUpdate = diff;
                output.WriteLine("#" + diff.TotalSeconds);
            }

            output.WriteLine($"0,Event={name}|{string.Join("|", items)}");
        }

        private string StringifyProps(Dictionary<string, string> props)
        {
            string[] propStrings = props.Select(x => x.Key + "=" + x.Value.Replace(",", "\\,")).ToArray();
            return string.Join(",", propStrings);
        }

        internal void Flush()
        {
            output.Flush();
        }
    }
}
