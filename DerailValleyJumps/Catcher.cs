using System;
using System.Collections.Generic;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

public class Catcher : MonoBehaviour
{
    public static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public Action<TrainCar> OnHit;
    float _nextAllowedInvocation = 0f;
    // NOTE: must be static to share between ALL catchers
    public static Dictionary<TrainCar, bool> CarsReadyForCatch = [];
    public static bool IsReadyToCatch = false;

    void Start()
    {
        var col = gameObject.GetComponent<BoxCollider>();

        if (col != null)
            col.isTrigger = true;
    }

    void OnDestroy()
    {
        // Logger.Log($"DESTROY {Track}");
    }

    /// <summary>
    /// This trigger collider determines if the incoming collider (which a train car has MANY) is suitable for catching.
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        float now = Time.time;

        // Logger.Log($"OnTriggerEnter collider={other.gameObject.name} allowed={now > _nextAllowedInvocation}");

        if (Main.settings.DisableCatching)
            return;

        if (now < _nextAllowedInvocation)
            return;

        // TODO: more reliable way
        var isBogie = other.gameObject.name == "[bogies]";
        if (!isBogie)
            return;

        TrainCar? car = other.transform.parent.GetComponent<TrainCar>();

        // Logger.Log($"Bogie={isBogie} Parent={parent} car={car}");

        if (car == null || !car.derailed)
            return;

        bool isReadyToCatch;

        if (!CarsReadyForCatch.TryGetValue(car, out isReadyToCatch))
            return;

        if (!isReadyToCatch)
            return;

        // Logger.Log($"CHECK upright={IsCarUpright(car, 45)} bogie={isBogie} parent={parent} car={car} derailed={car.derailed}");

        if (!TrainCarHelper.IsCarUpright(car, maxTiltDegrees: Main.settings.UprightDegrees))
            return;

        _nextAllowedInvocation = now + 0.5f;

        Logger.Log($"Car must be caught: {car} (bogie={isBogie} car={car} derailed={car.derailed})");

        OnHit.Invoke(car);

        // IsReadyToCatch = false;

        CarsReadyForCatch[car] = false;
    }

    /// <summary>
    /// A car must have left the tracks for it to be catchable.
    /// This trigger collider tracks this and tells ALL other catchers "this train is ready to catch".
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // Logger.Log($"OnTriggerExit collider={other} IsReadyToCatch={IsReadyToCatch}");

        if (Main.settings.DisableCatching)
            return;

        TrainCar? car = other.transform.parent.GetComponent<TrainCar>();

        if (car != null && car.derailed)
        {
            if (CarsReadyForCatch.ContainsKey(car) && CarsReadyForCatch[car] == true)
                return;

            Logger.Log($"Car ready for catching: {car} ({IsReadyToCatch} => true)");

            CarsReadyForCatch[car] = true;
        }
    }
}