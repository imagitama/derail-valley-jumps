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

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        Harmony? harmony = null;
        try
        {
            BindingsHelper.OnReady += () =>
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
        settings.Draw(modEntry);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Strong Jump"))
            settings.JumpForce = 30;
        if (GUILayout.Button("Default Jump"))
            settings.JumpForce = 20;
        if (GUILayout.Button("Weak Jump"))
            settings.JumpForce = 10;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Strong Spin"))
        {
            settings.TurnForce = 10;
            settings.FlipForce = 10;
            settings.RollForce = 10;
        }
        if (GUILayout.Button("Default Spin"))
        {
            settings.TurnForce = 5;
            settings.FlipForce = 5;
            settings.RollForce = 5;
        }
        if (GUILayout.Button("Weak Spin"))
        {
            settings.TurnForce = 1;
            settings.FlipForce = 1;
            settings.RollForce = 1;
        }
        GUILayout.EndHorizontal();

        List<BindingInfo> bindings = [
            settings.JumpBinding,
            settings.FlipFowardsBinding,
            settings.FlipBackwardsBinding,
            settings.TurnLeftBinding,
            settings.TurnRightBinding,
            settings.RollLeftBinding,
            settings.RollRightBinding,
        ];

        BindingsHelperUI.DrawBindings(bindings, OnUpdated: () => BindingsHelper.ApplyBindingDisables(bindings));
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
