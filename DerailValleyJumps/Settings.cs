using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

public static class Actions
{
    public static int Jump = 500;
    public static int FlipForwards = 501;
    public static int FlipBackwards = 502;
    public static int TurnLeft = 503;
    public static int TurnRight = 504;
    public static int RollLeft = 505;
    public static int RollRight = 506;
    public static int ReRail = 520;
}

public class Settings : UnityModManager.ModSettings, IDrawable
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    [Draw(Label = "Extra gravity for heavier landings")]
    public float ExtraGravity = 2f;
    [Draw(Label = "Draw extra debugging stuff")]
    public bool ExtraDebugging = false;
    [Draw(Label = "Jump Force (default 2,000,000)")]
    public float JumpForce = 2000000f;
    [Draw(Label = "Spin force (default 2,000,000)")]
    public float SpinForce = 2000000f;
    public BindingInfo JumpBinding = new BindingInfo("Jump", Actions.Jump, KeyCode.Space)
    {
        DisableDefault = true
    };
    public BindingInfo FlipFowardsBinding = new BindingInfo("Flip Forwards", Actions.FlipForwards, KeyCode.Keypad8)
    {
        DisableDefault = true
    };
    public BindingInfo FlipBackwardsBinding = new BindingInfo("Flip Backwards", Actions.FlipForwards, KeyCode.Keypad2)
    {
        DisableDefault = true
    };
    public BindingInfo TurnLeftBinding = new BindingInfo("Turn Left", Actions.TurnLeft, KeyCode.Keypad4)
    {
        DisableDefault = true
    };
    public BindingInfo TurnRightBinding = new BindingInfo("Turn Right", Actions.TurnRight, KeyCode.Keypad6)
    {
        DisableDefault = true
    };
    public BindingInfo RollLeftBinding = new BindingInfo("Roll Left", Actions.RollLeft, KeyCode.Keypad7)
    {
        DisableDefault = true
    };
    public BindingInfo RollRightBinding = new BindingInfo("Roll Right", Actions.RollRight, KeyCode.Keypad9)
    {
        DisableDefault = true
    };
    public BindingInfo ReRailBinding = new BindingInfo("Force Re-Rail", Actions.ReRail, KeyCode.F12);

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {
        ApplyBindingDisabling();
    }

    public void ApplyBindingDisabling()
    {
        List<BindingInfo> bindings = [
            JumpBinding,
            FlipFowardsBinding,
            FlipBackwardsBinding,
            TurnLeftBinding,
            TurnRightBinding,
            RollLeftBinding,
            RollRightBinding,
        ];

        foreach (var binding in bindings)
        {
            var conflicts = BindingsHelper.GetConflictingBindings(binding);

            if (binding.DisableDefault)
            {
                Logger.Log($"{binding.ActionId} Disabling {conflicts.Count} defaults: {string.Join(",", conflicts.Select(x => $"{x.actionDescriptiveName} ({x.controllerMap.controllerType})"))}");

                foreach (var conflict in conflicts)
                    conflict.enabled = false;
            }
            else
            {
                Logger.Log($"{binding.ActionId} Enabling {conflicts.Count} defaults: {string.Join(",", conflicts.Select(x => $"{x.actionDescriptiveName} ({x.controllerMap.controllerType})"))}");

                foreach (var conflict in conflicts)
                    conflict.enabled = true;
            }
        }
    }
}
