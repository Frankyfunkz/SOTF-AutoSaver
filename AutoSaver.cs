using Endnight.Utilities;
using FrankyModMenu;
using RedLoader;
using Sons.Characters;
using Sons.Gameplay.GameSetup;
using Sons.Gui;
using Sons.Save;
using SonsSdk;
using SUI;
using System.Collections;
using System.IO.Compression;
using TheForest.Utils;
using UnityEngine;
using static RedLoader.Coroutines;

namespace AutoSaver;

public class AutoSaver : SonsMod
{
    public AutoSaver()
    {

        // Uncomment any of these if you need a method to run on a specific update loop.
        //OnUpdateCallback = MyUpdateMethod;
        //OnLateUpdateCallback = MyLateUpdateMethod;
        //OnFixedUpdateCallback = MyFixedUpdateMethod;
        //OnGUICallback = MyGUIMethod;

        // Uncomment this to automatically apply harmony patches in your assembly.
        //HarmonyPatchAll = true;
    }

    public static bool _hasControl = false;
    public static bool AutoSaveTimerShouldRun = true;
    public static bool CoroShouldRestart = false;
    public static bool _firstStart = true;
    public static bool _returnedToTitle = false;
    public static bool CoroIsRunning = false;
    public static float floatValue;
    public static string playerGameName;
    

    protected override void OnInitializeMod()
    {
        // Do your early mod initialization which doesn't involve game or sdk references here
        Config.Init();
    }

    protected override void OnSdkInitialized()
    {
        // Do your mod initialization which involves game or sdk references here
        // This is for stuff like UI creation, event registration etc.
        AutoSaverUi.Create();
        //SettingsRegistry.CreateSettings(this, null, typeof(Config), callback: OnSettingsUiClosed);
    }

    protected override void OnGameStart()
    {
        // This is called once the player spawns in the world and gains control.
        Sons.Save.GameState gameState = new();
        playerGameName = gameState.GetGameName();
        WaitForLocalPlayer().RunCoro();

    }

    IEnumerator WaitForLocalPlayer()
    {

        static bool PlayerExists()
        {
            //RLog.Msg("waiting for LocalPlayer._instance...");
            return LocalPlayer._instance != null;
        }
        //Wait until LocalPlayer._instance is not null
        yield return CustomWaitUntil.WaitUntil(new Func<bool>(PlayerExists));
        //RLog.Msg("LocalPlayer._instance is not null. Continuing...");


        if (_hasControl == false)
        {
            //RLog.Msg("waiting for terrainOrflatcontact true, _hasControl is " + _hasControl );
            static bool PlayerHasControl()
            {
                return LocalPlayer.FpCharacter._terrainOrFlatContact == true;
            }
            //Wait until player has control
            yield return CustomWaitUntil.WaitUntil(new Func<bool>(PlayerHasControl));
            _hasControl = true;
            //RLog.Msg("terrainOrflatcontact true, set _hasControl to " + _hasControl);
            if (_firstStart == true)
            {
                SettingsRegistry.CreateSettings(this, null, typeof(Config), callback: OnSettingsUiClosed);
            }
            Config.UpdateSettings();
            _firstStart = false;
        }
        else
        {
            //RLog.Msg("_hasControl is" + _hasControl);
            if (_firstStart == true)
            {
                SettingsRegistry.CreateSettings(this, null, typeof(Config), callback: OnSettingsUiClosed);
            }
            Config.UpdateSettings();
            _firstStart = false;
        }
    }


    public static IEnumerator RestoreSaveName()
    {
        yield return new WaitForSeconds(3);
        Sons.Save.GameState gameState = new();
        gameState.SetGameName(playerGameName);
    }

    protected override void OnSonsSceneInitialized(ESonsScene sonsScene)
    {
        if (sonsScene == ESonsScene.Title)
        {
            if (!_firstStart)
            {
                
                _returnedToTitle = true;
                _hasControl = false;
                // Stopping AutoSave Coroutine on returning to title
                AutoSaveTimerShouldRun = false;
                RLog.Msg("Returned to title, disabled auto save");
                return;
            }
            else
            {
                return;
            }
        }
    }

    private void OnSettingsUiClosed()
    {
        if (!_returnedToTitle)
        {
            ClosedPauseMenu().RunCoro();
        }
        else
        {
            return;
        }
    }

    public static IEnumerator ClosedPauseMenu()
    {
        static bool NotInPauseMenu()
        {
            //RLog.Msg("waiting until pausemenu isactive returns false");
            return PauseMenu.IsActive == false;
        }
        yield return CustomWaitUntil.WaitUntil(new Func<bool>(NotInPauseMenu));
        //RLog.Msg("No pause menu instance found, updating settings");
        Config.UpdateSettings();
    }

    public static void SaveTimerMultiplier(float value)
    {
        if (float.TryParse(Config.SaveTimer.Value, out float selectedValue))
        {
            if (Config.TimerOptions.TryGetValue(selectedValue.ToString(), out float intervalSeconds))
            {
                floatValue = intervalSeconds; // Store the interval in seconds

            }
            else
            {
                RLog.Error("value for Config.SaveTimer did not pass as a valid float - Contact Franky");
            }
        }
    }



    public static IEnumerator AutoSaveTimer()
    {
        SaveGame();
        RLog.Msg("Running AutoSave every: " + Config.SaveTimer.Value + " minute(s) -> " + floatValue + " Seconds");
        while (AutoSaveTimerShouldRun)
        {
            CoroIsRunning = true;
            bool isOverWrite = Config.AutoSaveOverWrite.Value;
            float previousValue = floatValue;
            yield return new WaitForSeconds(floatValue);

            if (!Config.AutoSave.Value || CoroShouldRestart == true || AutoSaveTimerShouldRun == false || (floatValue != previousValue) || (isOverWrite != Config.AutoSaveOverWrite.Value))
            {
                //RLog.Msg("Stopping current AutoSaveTimer coro. CoroIsRunning and TimerShouldRun to false ");
                CoroIsRunning = false;
                
                yield break;
            }
            SaveGame();
        }
        

    }

    public static void SetAutoSave(bool onoff)
    {
        //RLog.Msg("SetAutoSave called");
        Config.AutoSave.Value = onoff;
        if (onoff)
        {
            //RLog.Msg("SetAutoSave true");
            //RLog.Msg("Starting new autosavetimer coro");
            AutoSaveTimerShouldRun = true;
            AutoSaveTimer().RunCoro();
            return;
        }
        else // When AutoSave is turned off
        {
            //RLog.Msg("SetAutoSave false, stopping autosavetimer coro");
            AutoSaveTimerShouldRun = false;

            return;
        }
    }

    public static void SaveAuto()
    {
        var saveType = GameSetupManager.GetSaveGameType();
        Sons.Save.GameState gameState = new();
        int saveID = (int)SaveGameManager.GetNewRandomIndex(saveType);
        gameState.SetGameName("AutoSave");
        SaveGameManager.Save(saveType, "AutoSave", saveID);
        RestoreSaveName().RunCoro();
    }
    public static void SaveOver()
    {
        var saveType = GameSetupManager.GetSaveGameType();
        Sons.Save.GameState gameState = new();
        var steamId = Sons.Save.SaveGameManager._steamUserSaveGameFolder;
        string saveFolderOver = steamId + "/" + saveType + "/0800842069";
        gameState.SetGameName("AutoSave(OverWrite)");
        SaveGameManager.Save(saveFolderOver, "AutoSave(OverWrite)", false);
        RestoreSaveName().RunCoro();
    }


    public static void SaveGame()
    {
        //RLog.Msg("SaveGame called");
        if (Config.AutoSave.Value)
        {
            if (Config.AutoSaveOverWrite.Value)
            {
                //RLog.Msg("Overwrite on");
                SonsTools.ShowMessage("Auto Save(Overwrite)", 3f);
                SaveOver();
            }
            else
            {
                //RLog.Msg("Overwrite off");
                SonsTools.ShowMessage("Auto Save", 3f);
                SaveAuto();
            }
        }
        else
        {
            //RLog.Msg("Config.AutoSave.Value is false or LocalPlayer.IsInWorld is false");
            return;
        }
    }
}