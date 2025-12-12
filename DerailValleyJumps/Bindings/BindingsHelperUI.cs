using System;
using System.Collections.Generic;
using System.Linq;
using DV.Interaction.Inputs;
using Rewired;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

[Serializable]
public static class BindingsHelperUI
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    private static int? _bindingIndexPressed;
    private static float _isPressedHintTimer = 0;
    private static int? _bindingIndexRecording;
    private static float _failsafeTimeLeft = 0f;

    public static void DrawBindings(List<BindingInfo> bindings, int? actionIdToAdd = null, Action? OnUpdated = null)
    {
        var controllers = BindingsHelper.GetAllControllers();

        if (controllers == null)
            return;

        GUILayout.BeginHorizontal();
        for (var i = 0; i < bindings.Count; i++)
        {
            var binding = bindings[i];

            GUILayout.BeginVertical(GUILayout.Width(300f));

            DrawBinding(binding, index: i, OnUpdated);

            bindings[i] = binding;

            if (binding.Removable)
                if (GUILayout.Button("Remove Binding"))
                    bindings.RemoveAt(i);

            GUILayout.EndVertical();

            if ((i + 1) % 3 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }
        GUILayout.EndHorizontal();

        if (actionIdToAdd != null)
            if (GUILayout.Button("Add Binding"))
            {
                bindings.Add(
                    new BindingInfo()
                    {
                        ActionId = actionIdToAdd.Value,
                        ControllerId = 0,
                        ControllerType = ControllerType.Keyboard,
                        ControllerName = BindingsHelper.GetControllerNameFromType(ControllerType.Keyboard) ?? "",
                        ButtonName = "Space",
                        ButtonId = BindingsHelper.GetButtonId(ControllerType.Keyboard, 0, "Space")
                    }
                );
            }
    }

    public static void DrawBinding(BindingInfo binding, int? index = null, Action? OnUpdated = null)
    {
        var controllers = BindingsHelper.GetAllControllers();

        if (controllers == null)
            return;

        var bold = new GUIStyle(GUI.skin.label);
        bold.fontStyle = FontStyle.Bold;

        GUILayout.Label($"{binding.Label}", bold);

        GUILayout.Label($"  Controller: {binding.ControllerName ?? "(none)"} ({(binding.ControllerType != null ? binding.ControllerType.Value : "none")})");

        ControllerType? newControllerType = binding.ControllerType;
        string? newControllerName = binding.ControllerName;
        int? newControllerId = binding.ControllerId;
        bool newDisableDefault = binding.DisableDefault;

        foreach (var controller in controllers)
        {
            // TODO: why is controller ID always 0
            var isNowChecked = GUILayout.Toggle(binding.ControllerName == controller.name, controller.name);

            if (isNowChecked && binding.ControllerName != controller.name)
            {
                Logger.Log($"[BindingsHelperUI] Binding select controller {binding.ControllerName} => {controller.name}");
                newControllerType = controller.type;
                newControllerName = controller.name;
                newControllerId = controller.id;
            }
        }

        GUILayout.Label($"  Button name: {binding.ButtonName ?? "(none)"} ({(binding.ButtonId != null ? binding.ButtonId.Value : "(none)")})");
        // var newButtonName = GUILayout.TextField(binding.ButtonName);
        // var newButtonId = BindingsHelper.GetButtonId(newControllerType, newControllerId, newButtonName);

        string? newButtonName = null;
        int? newButtonId = null;

        if (_bindingIndexRecording != null && _bindingIndexRecording == index && newControllerType != null)
        {
            if (GUILayout.Button("Stop Recording", GUILayout.Width(150f)))
            {
                _bindingIndexRecording = null;
            }

            GUILayout.Label($"  Waiting for a button press ({Mathf.CeilToInt(_failsafeTimeLeft)})...");

            if (_failsafeTimeLeft > 0f)
            {
                _failsafeTimeLeft -= Time.deltaTime * 0.5f; // OnGUI much faster
                if (_failsafeTimeLeft < 0f)
                {
                    Logger.Log("[BindingsHelperUI] Binding timer ended");
                    _failsafeTimeLeft = 0f;
                    _bindingIndexRecording = null;
                }
            }

            var result = BindingsHelper.GetAnyButtonPressedInfo(newControllerType.Value);

            if (result != null)
            {
                var (pressedButtonName, pressedButtonId) = result.Value;
                Logger.Log($"[BindingsHelperUI] User pressed controllerType={newControllerType} controllerName={newControllerName} name={pressedButtonName} id={pressedButtonId}");
                newButtonName = pressedButtonName;
                newButtonId = pressedButtonId;

                _bindingIndexRecording = null;
            }
        }
        else
        {
            if (GUILayout.Button("Record", GUILayout.Width(150f)))
            {
                Logger.Log("[BindingsHelperUI] Binding timer started");
                _bindingIndexRecording = index;
                _failsafeTimeLeft = 5f;
            }
        }

        if (index != null && _bindingIndexRecording == null && binding.ControllerType != null && binding.ControllerId != null && binding.ButtonId != null)
        {
            if (BindingsHelper.GetIsPressed(binding.ControllerType.Value, binding.ControllerId.Value, binding.ButtonId.Value))
            {
                _bindingIndexPressed = index;
                _isPressedHintTimer = Time.time + 1f;
            }

            if (_bindingIndexPressed != null && Time.time >= _isPressedHintTimer)
            {
                _bindingIndexPressed = null;
            }

            if (_bindingIndexPressed == index)
            {
                GUILayout.Label("Pressed!");
            }
        }

        var conflicts = BindingsHelper.GetConflictingBindings(binding);

        if (conflicts.Count > 0)
        {
            foreach (var conflict in conflicts)
                GUILayout.Label($"Conflicts with '{conflict.actionDescriptiveName}' ({conflict.controllerMap.controllerType})");

            newDisableDefault = GUILayout.Toggle(binding.DisableDefault, "Disable default");
        }

        if (binding.Clearable)
            if (GUILayout.Button("Clear Binding"))
            {
                newButtonId = null;
            }

        var scheduleOnUpdated = false;

        if (
            newControllerType != binding.ControllerType ||
            newControllerName != binding.ControllerName ||
            newControllerId != binding.ControllerId ||
            (newButtonName != null && newButtonName != binding.ButtonName) ||
            (newButtonId != null && newButtonId != binding.ButtonId) ||
            (newDisableDefault != binding.DisableDefault)
        )
        {
            scheduleOnUpdated = true;
            //             Logger.Log($"CHANGED!!! " +
            // $"  {binding.ControllerType} => {newControllerType} --- " +
            // $"  {binding.ControllerName} => {newControllerName} --- " +
            // $"  {binding.ControllerId} => {newControllerId} --- " +
            // $"  {binding.ButtonName} => {newButtonName} --- " +
            // $"  {binding.ButtonId} => {newButtonId}");

        }

        binding.ControllerType = newControllerType;
        binding.ControllerName = newControllerName;
        binding.ControllerId = newControllerId;
        binding.DisableDefault = newDisableDefault;

        if (newButtonName != null)
            binding.ButtonName = newButtonName;
        if (newButtonId != null)
            binding.ButtonId = newButtonId.Value;

        if (scheduleOnUpdated)
            OnUpdated?.Invoke();
    }
}
