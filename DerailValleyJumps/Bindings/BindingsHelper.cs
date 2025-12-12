using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DV.Interaction.Inputs;
using Rewired;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

[Serializable]
public class BindingInfo
{
    public BindingInfo()
    {
    }
    public BindingInfo(int actionId)
    {
        ActionId = actionId;
    }
    public BindingInfo(string label, int actionId, KeyCode keyCode)
    {
        Label = label;
        ControllerType = Rewired.ControllerType.Keyboard;
        ControllerId = 0;
        ControllerName = "Keyboard";
        ButtonId = BindingsHelper.GetButtonId(keyCode);
        ButtonName = keyCode.ToString();
        ActionId = actionId;
    }
    public string Label;
    public Rewired.ControllerType? ControllerType;
    public string? ControllerName; // not required but helpful
    public int? ControllerId;
    public string? ButtonName; // not required but helpful
    public int? ButtonId;
    public int? ActionId; // optional for non-standard bindings
    public bool Removable = false;
    public bool Clearable = false;
    public bool DisableDefault = false;

    public override bool Equals(object obj)
    {
        if (obj is BindingInfo other)
            return
                ControllerType == other.ControllerType &&
                ControllerName == other.ControllerName &&
                ControllerId == other.ControllerId &&
                ButtonName == other.ButtonName &&
                ButtonId == other.ButtonId &&
                ActionId == other.ActionId;
        return false;
    }

    public override string ToString()
    {
        return $"Binding(cType={ControllerType},cName={ControllerName},cId={ControllerId},bName={ButtonName},bId={ButtonId},aId={ActionId})";
    }

    public string GetLabel()
    {
        if (ControllerType == null || ButtonId == null)
            return "(none)";
        return $"{ControllerName}.{ButtonName}";
    }
}

public static class BindingsHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static bool IsReady => ReInput.isReady && InputManager.NewPlayer != null;

    public static event Action OnReady
    {
        add
        {
            void OnReInputReady()
            {
                Logger.Log($"[BindingsHelper] OnReady");
                value.Invoke();
            }

            if (IsReady)
                OnReInputReady();
            else
                ReInput.InitializedEvent += OnReInputReady;
        }
        remove
        {
            // TODO?
        }
    }

    public static int GetButtonId(ControllerType controllerType, int controllerId, string buttonName)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return -1;

        Controller controller = player.controllers.GetController(controllerType, controllerId);

        // TODO: cache this
        var result = controller?.ButtonElementIdentifiers.ToList().Find(x => x.name == buttonName);

        if (result == null)
            return -1;

        return result.id;
    }

    public static int GetButtonId(KeyCode keyCode)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return -1;

        Keyboard keyboard = (Keyboard)player.controllers.GetController<Keyboard>(0);

        var elementForKeyCode = keyboard.GetElementIdentifierByKeyCode(keyCode);

        if (elementForKeyCode == null)
            return -1;

        return elementForKeyCode.id;
    }

    public static bool GetIsPressed(ControllerType controllerType, int controllerId, int buttonId)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return false;

        Controller controller = player.controllers.GetController(controllerType, controllerId);

        var pressed = controller?.GetButtonById(buttonId);
        return pressed == true;
    }

    public static bool GetIsPressed(ControllerType controllerType, int controllerId, string buttonName)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return false;

        // TODO: cache this
        Controller controller = player.controllers.GetController(controllerType, controllerId);

        var buttonId = GetButtonId(controllerType, controllerId, buttonName);

        var pressed = controller?.GetButtonById(buttonId);
        return pressed == true;
    }

    public static bool GetIsPressed(List<BindingInfo> bindings, int actionId)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return false;

        // TODO: more performant way of doing this
        var bindingsForAction = bindings.Where(binding => binding.ActionId == actionId);

        foreach (var binding in bindingsForAction)
        {
            if (binding.ControllerType == null || binding.ControllerId == null || binding.ButtonId == null)
                continue;

            // TODO: cache this
            Controller controller = player.controllers.GetController(binding.ControllerType.Value, binding.ControllerId.Value);

            var pressed = controller?.GetButtonById(binding.ButtonId.Value);

            if (pressed == true)
                return true;
        }

        return false;
    }

    public static bool GetIsPressed(BindingInfo binding)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return false;

        if (binding.ControllerType == null || binding.ControllerId == null || binding.ButtonId == null)
            return false;

        // TODO: cache this
        Controller controller = player.controllers.GetController(binding.ControllerType.Value, binding.ControllerId.Value);

        var pressed = controller?.GetButtonById(binding.ButtonId.Value);

        // Logger.Log($"GetIsPressed {binding} pressed={pressed}");

        return pressed == true;
    }

    public static (string buttonName, int buttonId)? GetAnyButtonPressedInfo(ControllerType controllerType)
    {
        var controllerPollingInfo = ReInput.controllers.polling.PollControllerForFirstButtonDown(controllerType, 0);

        if (!controllerPollingInfo.success)
            return null;

        return (controllerPollingInfo.elementIdentifierName, controllerPollingInfo.elementIdentifierId);
    }

    public static string? GetControllerNameFromType(ControllerType controllerType)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return null;
        return player.controllers.Controllers.ToList().Find(x => x.type == controllerType).name;
    }

    public static List<Controller>? GetAllControllers() => InputManager.NewPlayer?.controllers.Controllers.ToList();

    public static List<ActionElementMap> GetConflictingBindings(BindingInfo ourBinding)
    {
        var player = InputManager.NewPlayer;
        if (player == null)
            return [];

        List<ActionElementMap> conflicts = [];

        foreach (ControllerMap controllerMap in player.controllers.maps.GetAllMaps())
        {
            var maps = controllerMap.GetButtonMaps();

            if (controllerMap.controllerId == ourBinding.ControllerId &&
                controllerMap.controllerType == ourBinding.ControllerType)
            {
                var elements = controllerMap.ElementMaps;

                foreach (var element in elements)
                {
                    if (element.elementIdentifierId == ourBinding.ButtonId)
                    {
                        conflicts.Add(element);
                    }
                }
            }


            // allMap.enabled = enabled;
        }

        return conflicts;
    }

    public static string? GetFriendlyName(int actionId)
    {
        var fields = typeof(InputManager.RewiredActionConsts).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(int)) continue;

            var value = (int)field.GetValue(null);
            if (value != actionId) continue;

            var attr = field.GetCustomAttribute<Rewired.Dev.ActionIdFieldInfoAttribute>();
            if (attr == null) return null;

            return attr.friendlyName;
        }

        return null;
    }

    public static string? GetCategoryName(int actionId)
    {
        var fields = typeof(InputManager.RewiredActionConsts).GetFields(BindingFlags.Public | BindingFlags.Static);

        foreach (var field in fields)
        {
            if (field.FieldType != typeof(int)) continue;

            var value = (int)field.GetValue(null);
            if (value != actionId) continue;

            var attr = field.GetCustomAttribute<Rewired.Dev.ActionIdFieldInfoAttribute>();
            if (attr == null) return null;

            return attr.categoryName;
        }

        return null;
    }
}