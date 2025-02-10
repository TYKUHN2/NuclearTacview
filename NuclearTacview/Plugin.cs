using BepInEx;
using BepInEx.Logging;
using NuclearOption.SavedMission;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if BEP6
using BepInEx.Unity.Mono;
#endif

namespace NuclearTacview;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("NuclearOption.exe")]
public class Plugin: BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private Recorder? recorder;

    public Plugin()
    {
        Logger = base.Logger;

        MissionManager.onMissionStart += OnMissionLoad;
        LoadingManager.MissionUnloaded += OnMissionUnload;
    }

    private void Awake()
    {
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }

    private void Update() => recorder?.Update(Time.deltaTime);

    private void OnMissionLoad(Mission mission)
    {
        recorder = new Recorder(mission);
    }

    private void OnMissionUnload()
    {
        recorder = null;
    }

    private class Recorder
    {
        private readonly DateTime startDate;
        private DateTime curTime;
        private ACMIWriter writer;
        private readonly Dictionary<int, ACMIUnit> objects = [];

        internal Recorder(Mission mission)
        {
            startDate = DateTime.Today + TimeSpan.FromHours(mission.environment.timeOfDay);
            curTime = startDate;

            writer = new ACMIWriter(startDate);
        }

        internal void Update(float delta)
        {
            curTime += TimeSpan.FromSeconds(delta);

            foreach (var acmi in objects.Values)
                if (acmi.unit.disabled)
                    objects.Remove(acmi.id);

            Unit[] units = FindObjectsByType<Unit>(FindObjectsSortMode.None);
            foreach (var unit in units)
            {
                if (!unit.networked || unit.disabled)
                    continue;

                bool isNew = false;
                if (!objects.TryGetValue(unit.persistentID, out ACMIUnit acmi))
                {
                    switch (unit)
                    {
                        case Aircraft:
                            acmi = new ACMIAircraft((Aircraft)unit);
                            break;
                        case Missile:
                            acmi = new ACMIMissile((Missile)unit);
                            break;
                        case GroundVehicle:
                            acmi = new ACMIGroundVehicle((GroundVehicle)unit);
                            break;
                        case Building:
                            acmi = new ACMIBuilding((Building)unit);
                            break;
                        default:
                            continue;
                    }

                    objects.Add(unit.persistentID, acmi);
                    isNew = true;

                    acmi.OnEvent += WriteEvent;
                }

                Dictionary<string, string> props = acmi.Update();
                if (isNew)
                    props = props.Concat(acmi.Init()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                if (unit.IsLocalPlayer)
                    Logger.LogInfo(props["T"]);

                writer.UpdateObject(acmi, curTime, props);
            }

            writer.Flush();
        }

        private void WriteEvent(string name, int[] ids, string text)
        {
            writer.WriteEvent(curTime, name, [..ids.Select(a => a.ToString()), text]);
            writer.Flush();
        }
    }
}
