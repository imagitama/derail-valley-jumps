using System;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyJumps;

public class CatcherCollider : MonoBehaviour
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

        if (now > _nextAllowedInvocation)
        {
            // TODO: more reliable way
            var isBogie = other.gameObject.name == "[bogies]";

            if (isBogie)
            {
                TrainCar? car = null;
                var parent = other.transform.parent;

                if (parent != null)
                    car = parent.GetComponent<TrainCar>();

                Logger.Log($"Bogie={isBogie} Parent={parent} car={car}");

                if (car != null && car.derailed)
                {
                    _nextAllowedInvocation = now + 0.5f;

                    Logger.Log($"Invoke");

                    OnHit.Invoke(car);

                    IsReadyToCatch = false;
                }
            }
        }
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
            Logger.Log($"OnTriggerExit car={car}");
            IsReadyToCatch = true;
        }
    }

    // void OnHit(TrainCar car)
    // {
    //     var speedBeforeRerail = car.rb.velocity;

    //     Logger.Log($"Catch has happened! Rerail '{car}' onto '{Track}' speed={speedBeforeRerail}");

    //     TrainCarHelper.RerailTrainWithoutVelocity(car, car.transform.forward);

    //     Logger.Log($"Rerail complete");
    // }
}