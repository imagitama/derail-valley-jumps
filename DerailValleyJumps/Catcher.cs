using System;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

public class Catcher : MonoBehaviour
{
    public static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public RailTrack Track;
    public BoxCollider col;
    public Action<TrainCar> OnHit;

    void Start()
    {
        col = gameObject.GetComponent<BoxCollider>();

        if (col != null)
            col.isTrigger = true;
    }

    void OnDestroy()
    {
        // Logger.Log($"DESTROY {Track}");
    }

    float _nextAllowedInvocation = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (!IsReadyToCatch)
            return;

        float now = Time.time;

        // Logger.Log($"OnTriggerEnter collider={other.gameObject.name} allowed={now > _nextAllowedInvocation}");

        if (now < _nextAllowedInvocation)
            return;

        // TODO: more reliable way
        var isBogie = other.gameObject.name == "[bogies]";

        if (!isBogie)
            return;

        TrainCar? car = null;
        var parent = other.transform.parent;

        if (parent != null)
            car = parent.GetComponent<TrainCar>();

        // Logger.Log($"Bogie={isBogie} Parent={parent} car={car}");

        if (car == null || !car.derailed)
            return;

        // Logger.Log($"CHECK upright={IsCarUpright(car, 45)} bogie={isBogie} parent={parent} car={car} derailed={car.derailed}");

        if (!IsCarUpright(car, maxTiltDegrees: Main.settings.UprightDegrees))
            return;

        _nextAllowedInvocation = now + 0.5f;

        Logger.Log($"Invoking... bogie={isBogie} parent={parent} car={car} derailed={car.derailed}");

        OnHit.Invoke(car);

        IsReadyToCatch = false;
    }

    bool IsCarUpright(TrainCar car, float maxTiltDegrees)
    {
        var up = car.transform.up;
        float angle = Vector3.Angle(up, Vector3.up);

        return angle <= maxTiltDegrees;
    }


    public static bool IsReadyToCatch = false;

    void OnTriggerExit(Collider other)
    {
        if (IsReadyToCatch)
            return;

        TrainCar? car = null;
        var parent = other.transform.parent;

        if (parent != null)
            car = parent.GetComponent<TrainCar>();

        if (car != null && car.derailed)
        {
            // Logger.Log($"OnTriggerExit car={car}");
            IsReadyToCatch = true;
        }
    }
}