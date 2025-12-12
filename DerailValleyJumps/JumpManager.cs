using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace DerailValleyJumps;

public class JumpManager
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private GameObject? _updateDriver;
    private List<CatcherCollider> _catchers = [];
    private float? _recentlyJumpedTimer;
    private bool hasStarted = false;

    public void Start()
    {
        if (hasStarted)
            return;

        Logger.Log($"Starting... jump={Main.settings.JumpForce} spin={Main.settings.SpinForce} gravity={Main.settings.ExtraGravity}");

        _updateDriver = new GameObject("DerailValleyJumps_UpdateDriver");
        UnityEngine.Object.DontDestroyOnLoad(_updateDriver);
        var comp = _updateDriver.AddComponent<UpdateDriver>();
        comp.OnFrame = OnFrame;
        comp.OnLateFrame = OnLateFrame;

        AddToRailTracks();

        hasStarted = true;
    }

    public void Stop()
    {
        Logger.Log("Stopping...");

        RemoveFromRailTracks();

        GameObject.Destroy(_updateDriver);
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

        HandleReRail();
        HandleJump();
        HandleFlipForwards();
        HandleFlipBackwards();
        HandleTurnLeft();
        HandleTurnRight();
        HandleRollLeft();
        HandleRollRight();
    }

    void HandleReRail()
    {
        var binding = Main.settings.ReRailBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (IsNewPress(binding, isPressed))
        {
            ManuallyReRailPlayerCar();
        }
    }

    void HandleJump()
    {
        var binding = Main.settings.JumpBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (IsNewPress(binding, isPressed))
            JumpTrainCar();
    }

    void HandleFlipForwards()
    {
        var binding = Main.settings.FlipFowardsBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(PlayerManager.Car.transform.right);
    }

    void HandleFlipBackwards()
    {
        var binding = Main.settings.FlipBackwardsBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(-PlayerManager.Car.transform.right);
    }

    void HandleTurnLeft()
    {
        var binding = Main.settings.TurnLeftBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(-PlayerManager.Car.transform.up);
    }

    void HandleTurnRight()
    {
        var binding = Main.settings.TurnRightBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(PlayerManager.Car.transform.up);
    }

    void HandleRollLeft()
    {
        var binding = Main.settings.RollLeftBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(-PlayerManager.Car.transform.forward);
    }

    void HandleRollRight()
    {
        var binding = Main.settings.RollRightBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(PlayerManager.Car.transform.forward);
    }

    void OnLateFrame()
    {
        if (PlayerManager.Car == null)
            return;

        if (Main.settings.ExtraGravity > 0)
            PlayerManager.Car.rb.AddForce(Physics.gravity * Main.settings.ExtraGravity * PlayerManager.Car.rb.mass);

        // if (spinTimeRemaining > 0f)
        // {
        //     spinTimeRemaining -= Time.fixedDeltaTime;

        //     var rb = PlayerManager.Car.rb;
        //     var torque = spinDirection * Main.settings.SpinForce;

        //     rb.AddTorque(torque, ForceMode.Acceleration);
        // }
    }

    void JumpTrainCar()
    {
        Logger.Log("Jump!");

        var car = PlayerManager.Car;

        if (car == null)
            return;

        car.Derail(suppressDerailSound: true);
        car.rb.AddForce(Vector3.up * Main.settings.JumpForce, ForceMode.Impulse);

        _recentlyJumpedTimer = Time.time + 0.5f;

        CatcherCollider.IsReadyToCatch = false;
    }

    // float spinTimeRemaining = 0f;
    // Vector3 spinDirection;

    void SpinTrainCar(Vector3 direction)
    {
        Logger.Log("Spin!");

        var car = PlayerManager.Car;

        if (car == null)
            return;

        if (!car.derailed)
            return;

        var torque = direction * Main.settings.SpinForce;
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
                if (Main.settings.ExtraDebugging != true)
                {
                    var mr = colliderObj.gameObject.GetComponent<MeshRenderer>();
                    GameObject.Destroy(mr);
                }

                var newComp = colliderObj.gameObject.AddComponent<CatcherCollider>();
                newComp.Track = track;
                newComp.OnHit = car => OnHit(car, track);

                _catchers.Add(newComp);
            }
        }

        Logger.Log("Addition done");
    }

    void ManuallyReRailPlayerCar()
    {
        Logger.Log("Manually rerail player car");

        var car = PlayerManager.Car;
        if (car != null)
        {
            TrainCarHelper.RerailTrainWithoutVelocity(car, car.transform.forward);
        }
    }

    void OnHit(TrainCar car, RailTrack track)
    {
        var speedBeforeRerail = car.rb.velocity;

        Logger.Log($"Catch has happened! Rerail '{car}' onto '{track}' speed={speedBeforeRerail}");

        TrainCarHelper.RerailTrainWithoutVelocity(car, car.transform.forward);

        Logger.Log($"Rerail complete");
    }

    void RemoveFromRailTracks()
    {
        var allTracks = RailTrackRegistry.RailTracks.ToList();

        Logger.Log($"Removing from {allTracks.Count} railtracks...");

        foreach (var railTrack in allTracks)
        {
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
