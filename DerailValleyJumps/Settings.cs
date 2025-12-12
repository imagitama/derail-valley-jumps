using System.Collections.Generic;
using System.Linq;
using DerailValleyBindingHelper;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

public static class Actions
{
    public const int Jump = 500;
    public const int FlipForwards = 501;
    public const int FlipBackwards = 502;
    public const int TurnLeft = 503;
    public const int TurnRight = 504;
    public const int RollLeft = 505;
    public const int RollRight = 506;
    public const int ReRail = 520;
}

public class Settings : UnityModManager.ModSettings, IDrawable
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    [Draw(Label = "A tiny delay before actually rerailing")]
    public float RerailDelay = 0.1f;
    [Draw(Label = "Extra gravity for heavier landings")]
    public float ExtraGravity = 2f;
    [Draw(Label = "Draw extra debugging stuff")]
    public bool ExtraDebugging = false;
    [Draw(Label = "Jump Force x100000 (default 20)")]
    public float JumpForce = 150f;
    [Draw(Label = "Flip force x100000 (default 20)")]
    public float FlipForce = 40f;
    [Draw(Label = "Turn force x100000 (default 40)")]
    public float TurnForce = 20f;
    [Draw(Label = "Roll force x100000 (default 40)")]
    public float RollForce = 20f;
    [Draw(Label = "What is the limit for what is upright (degrees, default 45)")]
    public float UprightDegrees = 45f;
    public BindingInfo JumpBinding = new BindingInfo("Jump", Actions.Jump, KeyCode.Space)
    {
        DisableDefault = true
    };
    public BindingInfo FlipFowardsBinding = new BindingInfo("Flip Forwards", Actions.FlipForwards, KeyCode.Keypad8)
    {
        DisableDefault = true
    };
    public BindingInfo FlipBackwardsBinding = new BindingInfo("Flip Backwards", Actions.FlipBackwards, KeyCode.Keypad2)
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

        BindingsHelper.ApplyBindingDisables(bindings);
    }
}
