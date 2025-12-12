using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System;

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
            SpinTrainCar(binding.ActionId!.Value); // PlayerManager.Car.transform.right;
    }

    void HandleFlipBackwards()
    {
        var binding = Main.settings.FlipBackwardsBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(binding.ActionId!.Value); // -PlayerManager.Car.transform.right
    }

    void HandleTurnLeft()
    {
        var binding = Main.settings.TurnLeftBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(binding.ActionId!.Value); // -PlayerManager.Car.transform.up
    }

    void HandleTurnRight()
    {
        var binding = Main.settings.TurnRightBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(binding.ActionId!.Value); // PlayerManager.Car.transform.up
    }

    void HandleRollLeft()
    {
        var binding = Main.settings.RollLeftBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(binding.ActionId!.Value); // -PlayerManager.Car.transform.forward
    }

    void HandleRollRight()
    {
        var binding = Main.settings.RollRightBinding;
        if (binding.ButtonId == null) return;

        bool isPressed = BindingsHelper.GetIsPressed(binding);

        if (isPressed)
            SpinTrainCar(binding.ActionId!.Value); // PlayerManager.Car.transform.forward
    }

    void OnLateFrame()
    {
        if (PlayerManager.Car == null)
            return;

        if (Main.settings.ExtraGravity > 0)
            PlayerManager.Car.rb.AddForce(Physics.gravity * Main.settings.ExtraGravity * PlayerManager.Car.rb.mass);
    }

    void JumpTrainCar()
    {
        // Logger.Log("Jump!");

        var car = PlayerManager.Car;

        if (car == null)
            return;

        car.Derail(suppressDerailSound: true);
        car.rb.AddForce(Vector3.up * (Main.settings.JumpForce * 10000), ForceMode.Impulse);

        Catcher.IsReadyToCatch = false;
    }

    void SpinTrainCar(int actionId)
    {
        // Logger.Log("Spin!");

        var car = PlayerManager.Car;

        if (car == null)
            return;

        if (!car.derailed)
            return;

        Vector3 direction;
        float force;

        switch (actionId)
        {
            case Actions.FlipForwards:
                direction = PlayerManager.Car.transform.right;
                force = Main.settings.FlipForce;
                break;
            case Actions.FlipBackwards:

                direction = -PlayerManager.Car.transform.right;
                force = Main.settings.FlipForce;
                break;
            case Actions.TurnLeft:
                direction = -PlayerManager.Car.transform.up;
                force = Main.settings.TurnForce;
                break;
            case Actions.TurnRight:
                direction = PlayerManager.Car.transform.up;
                force = Main.settings.TurnForce;
                break;
            case Actions.RollLeft:
                direction = -PlayerManager.Car.transform.forward;
                force = Main.settings.RollForce;
                break;
            case Actions.RollRight:
                direction = PlayerManager.Car.transform.forward;
                force = Main.settings.RollForce;
                break;
            default:
                throw new Exception($"Cannot do action: {actionId}");
        }

        var torque = direction * (force * 10000);
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

                var newComp = colliderObj.gameObject.AddComponent<Catcher>();
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
        if (_updateDriver == null)
            return;
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
