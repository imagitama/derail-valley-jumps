using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;
using DerailValleyBindingHelper;

namespace DerailValleyJumps;

#if DEBUG
[EnableReloading]
#endif
public static class Main
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;
    public static JumpManager jumpManager;
    // callbacks
    public static Action<TrainCar> OnJump;
    public static Action<TrainCar, int> OnSpin;
    public static Action<TrainCar, RailTrack> OnCatch;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;



        Harmony? harmony = null;
        try
        {
            BindingHelper.OnReady += () =>
            {
                settings = Settings.Load<Settings>(modEntry);

                BindingsAPI.RegisterBindings(Main.ModEntry, [
                    settings.JumpBinding,
                    settings.FlipFowardsBinding,
                    settings.FlipBackwardsBinding,
                    settings.TurnLeftBinding,
                    settings.TurnRightBinding,
                    settings.RollLeftBinding,
                    settings.RollRightBinding
                ]);

                settings.ApplyBindingDisabling();

                modEntry.OnGUI = OnGUI;
                modEntry.OnSaveGUI = OnSaveGUI;

                jumpManager = new JumpManager();

                if (PlayerManager.Car != null)
                    jumpManager.Start();

                harmony = new Harmony(modEntry.Info.Id);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                ModEntry.Logger.Log("DerailValleyJumps started");
            };
        }
        catch (Exception ex)
        {
            ModEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        modEntry.OnUnload = Unload;
        return true;
    }

    static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        GUILayout.Label("All default values are tested on a DH4");

        settings.Draw(modEntry);

        List<BindingInfo> bindings = [
            settings.JumpBinding,
            settings.FlipFowardsBinding,
            settings.FlipBackwardsBinding,
            settings.TurnLeftBinding,
            settings.TurnRightBinding,
            settings.RollLeftBinding,
            settings.RollRightBinding,
        ];

        BindingHelperUI.DrawBindings(bindings, OnUpdated: () => BindingHelper.ApplyBindingDisables(bindings));
    }

    static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Save(modEntry);
    }

    private static bool Unload(UnityModManager.ModEntry entry)
    {
        jumpManager?.Stop();

        ModEntry.Logger.Log("DerailValleyJumps stopped");
        return true;
    }
}
