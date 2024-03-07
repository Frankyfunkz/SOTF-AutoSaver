using Endnight.Utilities;
using RedLoader;
using System.Collections;
using UnityEngine;
using static RedLoader.Coroutines;

namespace AutoSaver;

public static class Config
{

    public static ConfigCategory Category { get; private set; }
    public static ConfigEntry<bool> AutoSave { get; private set; }
    public static ConfigEntry<bool> AutoSaveOverWrite { get; private set; }
    public static ConfigEntry<string> SaveTimer { get; private set; }
    public static Dictionary<string, float> TimerOptions = new()
    {
        { "1", 60f }, { "2", 120f }, { "3", 180 }, { "4", 240f }, { "5", 300f }, 
        { "10", 600f }, { "15", 900f },  { "20", 1200f }, { "30", 1800f }, { "45", 2700f }, { "60", 3600f }
    };

    public static void Init()
    {
        Category = ConfigSystem.CreateFileCategory("AutoSaver", "AutoSaver", "AutoSaver.cfg");
        
        //string defaultMultiplierKey = "5";

        AutoSave = Category.CreateEntry("AutoSaveToggle", false, "Enable/Disable Auto Saving", "", false);
        AutoSaveOverWrite = Category.CreateEntry("AutoSaveOverWrite", false, "Overwrite Auto Save Slot", "If enabled overwrite autosave slot, if disabled make new save every time", false);
        SaveTimer = Category.CreateEntry("SaveTimer", "15", "AutoSave interval in MINUTES", "", false);
        SaveTimer.SetOptions(TimerOptions.Keys.ToArray());

    }



    public static IEnumerator ConfigAutoSaveTimer()
    {
        if (AutoSaver.CoroIsRunning)
        {
            //RLog.Msg("Stopping current coro from update settings");
            AutoSaver.CoroShouldRestart = true;
            yield return new WaitForSeconds(AutoSaver.floatValue + 5f);
            AutoSaver.CoroShouldRestart = false;
            //RLog.Msg("Is Coroutine running: " + AutoSaver.CoroIsRunning);
            AutoSaver.SetAutoSave(AutoSave.Value);
        }
        else
        {
            AutoSaver.CoroShouldRestart = false;
            //RLog.Msg("Is Coroutine running: " + AutoSaver.CoroIsRunning);
            AutoSaver.SetAutoSave(AutoSave.Value);
        }

        
    }


    public static void UpdateSettings()
    {
        //RLog.Msg("AutoSave = " + AutoSave.Value);
        //RLog.Msg("SaveTimer = " + SaveTimer.Value);
        //RLog.Msg("AutoSaveOverWrite = " + AutoSaveOverWrite.Value);

        if (float.TryParse(SaveTimer.Value, out float floatValue) && (floatValue != AutoSaver.floatValue))
        {
            AutoSaver.SaveTimerMultiplier(floatValue);
        }
        else
        {
            RLog.Msg("Invalid or missing savetimer value, contact Franky.");
        }
        ConfigAutoSaveTimer().RunCoro();
    }
}