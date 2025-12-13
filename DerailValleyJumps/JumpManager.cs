using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;
using DerailValleyBindingHelper;

namespace DerailValleyJumps;

public class JumpManager
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private GameObject? _updateDriverGO;
    private UpdateDriver? _updateDriver;
    private List<Catcher> _catchers = [];
    private bool _hasStarted = false;

    public void Start()
    {
        if (_hasStarted)
            return;

        Logger.Log($"Starting... jump={Main.settings.JumpForce} flip={Main.settings.FlipForce} turn={Main.settings.TurnForce} roll={Main.settings.RollForce} gravity={Main.settings.ExtraGravity}");

        _updateDriverGO = new GameObject("DerailValleyJumps_UpdateDriver");
        UnityEngine.Object.DontDestroyOnLoad(_updateDriverGO);
        _updateDriver = _updateDriverGO.AddComponent<UpdateDriver>();
        _updateDriver.OnFrame = OnFrame;
        _updateDriver.OnLateFrame = OnLateFrame;

        AddToRailTracks();

        _hasStarted = true;
    }

    public void Stop()
    {
        Logger.Log("Stopping...");

        RemoveFromRailTracks();

        GameObject.Destroy(_updateDriverGO);
    }

    Dictionary<object, bool> _lastPressed = new Dictionary<object, bool>();

    bool IsNewPress(object key, bool isPressed)
    {
        bool wasPressed = false;
        _lastPressed.TryGetValue(key, out wasPressed);

        bool isNew = isPressed && !wasPressed;

        _lastPressed[key] = isPressed;
        return isNew;
    }

    void OnFrame()
    {
        if (!Application.isFocused)
            return;

        HandleJump();
        HandleFlipForwards();
        HandleFlipBackwards();
        HandleTurnLeft();
        HandleTurnRight();
        HandleRollLeft();
        HandleRollRight();
    }

    void HandleJump()
    {
        var binding = Main.settings.JumpBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        JumpTrainCar(isPressed);
    }

    void HandleFlipForwards()
    {
        var binding = Main.settings.FlipFowardsBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        SpinTrainCar(binding.ActionId!.Value, isPressed);
    }

    void HandleFlipBackwards()
    {
        var binding = Main.settings.FlipBackwardsBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        SpinTrainCar(binding.ActionId!.Value, isPressed);
    }

    void HandleTurnLeft()
    {
        var binding = Main.settings.TurnLeftBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        SpinTrainCar(binding.ActionId!.Value, isPressed);
    }

    void HandleTurnRight()
    {
        var binding = Main.settings.TurnRightBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        SpinTrainCar(binding.ActionId!.Value, isPressed);
    }

    void HandleRollLeft()
    {
        var binding = Main.settings.RollLeftBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        SpinTrainCar(binding.ActionId!.Value, isPressed);
    }

    void HandleRollRight()
    {
        var binding = Main.settings.RollRightBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingHelper.GetIsPressed(binding);

        SpinTrainCar(binding.ActionId!.Value, isPressed);
    }

    void OnLateFrame()
    {
        if (PlayerManager.Car == null)
            return;

        if (Main.settings.ExtraGravity > 0)
            PlayerManager.Car.rb.AddForce(Physics.gravity * Main.settings.ExtraGravity * PlayerManager.Car.rb.mass);
    }

    private float? _jumpPressStart;

    void JumpTrainCar(bool isPressed)
    {
        // Logger.Log("Jump!");

        var car = PlayerManager.Car;
        if (car == null)
            return;

        if (!isPressed)
        {
            _jumpPressStart = null;
            return;
        }

        if (_jumpPressStart == null)
        {
            Main.OnJump?.Invoke(car);
            _jumpPressStart = Time.time;
        }

        if (!car.derailed)
            car.Derail(suppressDerailSound: true);

        float heldTime = Time.time - _jumpPressStart.Value;
        float strength01 = Mathf.Clamp01(heldTime / RampTime);

        float baseForce = Main.settings.JumpForce;
        float scaledForce = baseForce * strength01;
        var force = Vector3.up * (scaledForce * 10000);

        car.rb.AddForce(force, ForceMode.Impulse);

        Catcher.IsReadyToCatch = false;
    }

    static readonly Dictionary<int, float> actionPressStart = new();
    const float RampTime = 0.5f;

    void SpinTrainCar(int actionId, bool isPressed)
    {
        var car = PlayerManager.Car;
        if (car == null || !car.derailed)
            return;

        if (!isPressed)
        {
            actionPressStart.Remove(actionId);
            return;
        }

        if (!actionPressStart.TryGetValue(actionId, out var startTime))
        {
            Main.OnSpin?.Invoke(car, actionId);
            startTime = Time.time;
            actionPressStart[actionId] = startTime;
        }

        float heldTime = Time.time - startTime;
        float strength01 = Mathf.Clamp01(heldTime / RampTime);

        Vector3 direction;
        float baseForce;

        switch (actionId)
        {
            case Actions.FlipForwards:
                direction = car.transform.right;
                baseForce = Main.settings.FlipForce;
                break;
            case Actions.FlipBackwards:
                direction = -car.transform.right;
                baseForce = Main.settings.FlipForce;
                break;
            case Actions.TurnLeft:
                direction = -car.transform.up;
                baseForce = Main.settings.TurnForce;
                break;
            case Actions.TurnRight:
                direction = car.transform.up;
                baseForce = Main.settings.TurnForce;
                break;
            case Actions.RollLeft:
                direction = -car.transform.forward;
                baseForce = Main.settings.RollForce;
                break;
            case Actions.RollRight:
                direction = car.transform.forward;
                baseForce = Main.settings.RollForce;
                break;
            default:
                throw new Exception($"Cannot do action: {actionId}");
        }

        float scaledForce = baseForce * strength01;
        var torque = direction * (scaledForce * 10000f);

        car.rb.AddTorque(torque, ForceMode.Impulse);
    }

    public void AddToRailTracks()
    {
        var allTracks = RailTrackRegistry.RailTracks.ToList();

        Logger.Log($"Adding to {allTracks.Count} railtracks...");

        foreach (var track in allTracks)
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.transform.localScale = new Vector3(2f, 1f, 2f);

            RailTrack.MakeColliders(track, prefab, hideFlags: HideFlags.None);

            var colliders = track.transform.GetComponentsInChildren<Collider>();

            foreach (Collider colliderObj in colliders)
            {
                colliderObj.isTrigger = true;

                // fix targettable by teleport cursor
                colliderObj.gameObject.layer = (int)DVLayer.No_Teleport_Interaction;

                if (Main.settings.ExtraDebugging != true)
                {
                    var mr = colliderObj.gameObject.GetComponent<MeshRenderer>();
                    GameObject.Destroy(mr);
                }

                var newComp = colliderObj.gameObject.AddComponent<Catcher>();
                newComp.OnHit = car => OnHit(car, track);

                _catchers.Add(newComp);
            }
        }

        Logger.Log("Addition done");
    }

    void OnHit(TrainCar car, RailTrack track)
    {
        if (_updateDriver == null)
            return;

        Main.OnCatch?.Invoke(car, track);

        _updateDriver.StartCoroutine(DelayedRerail(car, track));
    }

    IEnumerator DelayedRerail(TrainCar car, RailTrack track)
    {
        var speedBeforeRerail = car.rb.velocity;

        Logger.Log($"Catch has happened! Rerail '{car}' onto '{track}' speed={speedBeforeRerail}");

        yield return new WaitForSeconds(Main.settings.RerailDelay);

        Logger.Log("Rerailing...");

        TrainCarHelper.RerailTrainWithoutVelocity(car, car.transform.forward);

        Logger.Log("Rerail complete");
    }

    void RemoveFromRailTracks()
    {
        var allTracks = RailTrackRegistry.RailTracks.ToList();

        Logger.Log($"Removing from {allTracks.Count} railtracks...");

        foreach (var railTrack in allTracks)
        {
            // TODO: something more reliable
            var child = railTrack.transform.Find("COLLIDERS");

            if (child != null)
                GameObject.Destroy(child.gameObject);
        }

        var snapshot = _catchers.ToArray();
        foreach (var catcher in snapshot)
        {
            if (catcher != null)
                GameObject.Destroy(catcher);
        }

        _catchers.Clear();

        Logger.Log($"Removal complete");
    }

}
