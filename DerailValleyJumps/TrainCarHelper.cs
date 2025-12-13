using System.Linq;
using DV;
using DV.Customization;
using DV.Damage;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;
using DV.TerrainSystem;
using System.Reflection;
using DV.MultipleUnit;
using System.Collections;
using DV.VRTK_Extensions;

namespace DerailValleyJumps;

public static class TrainCarHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static Vector3? GetApproxStandardJatoPosition(TrainCar trainCar, bool isRear = false)
    {
        var coupler = isRear ? trainCar.rearCoupler : trainCar.frontCoupler;
        var collider = coupler.GetComponent<BoxCollider>();

        Bounds b = collider.bounds;

        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3 worldBottomRight = new Vector3(c.x + e.x, c.y - e.y, c.z - e.z);
        Vector3 bottomRight = trainCar.transform.InverseTransformPoint(worldBottomRight);

        return bottomRight;
    }

    public static Vector3? GetApproxStandardRearJatoPosition(TrainCar trainCar)
    {
        return GetApproxStandardJatoPosition(trainCar, isRear: true);
    }

    public static Vector3? GetApproxStandardFrontJatoPosition(TrainCar trainCar)
    {
        return GetApproxStandardJatoPosition(trainCar, isRear: false);
    }

    static Vector3 GetTrackForward(RailTrack track, float t)
    {
        var curve = track.curve;
        float dt = 0.001f;

        float t2 = Mathf.Clamp01(t + dt);

        var p1 = curve.GetPointAt(t);
        var p2 = curve.GetPointAt(t2);

        return (p2 - p1).normalized;
    }

    // latest version
    // public static void RerailTrain(TrainCar trainCar, bool isReverse = false)
    // {
    //     var (closestTrack, point) = RailTrack.GetClosest(trainCar.transform.position);

    //     if (point == null)
    //         return;

    //     var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

    //     float t = closestTrack.curve.GetClosestT(rerailPos);

    //     var forward = GetTrackForward(closestTrack, t);

    //     if (isReverse)
    //         forward = -forward;

    //     void OnRerailed()
    //     {
    //         trainCar.brakeSystem.SetHandbrakePosition(0);
    //         trainCar.OnRerailed -= OnRerailed;
    //     }

    //     trainCar.OnRerailed += OnRerailed;

    //     if (trainCar.derailed)
    //         trainCar.Rerail(closestTrack, rerailPos, forward);
    //     else
    //         trainCar.SetTrack(closestTrack, rerailPos, forward);
    // }

    static Vector3 GetCurveTangent(RailTrack track, float t)
    {
        var curve = track.curve;
        const float dt = 0.001f;

        float t2 = Mathf.Clamp01(t + dt);

        var p1 = curve.GetPointAt(t);
        var p2 = curve.GetPointAt(t2);

        return (p2 - p1).normalized;
    }


    static Vector3 ResolveForward(RailTrack current, RailTrack prevTrack, Vector3 tangent)
    {
        // If prevTrack connects to current as an OUT branch, 
        // then the forward direction on current goes in that direction.
        if (current.IsTrackOutBranch(prevTrack))
        {
            return tangent;
        }

        // Otherwise, forward is the opposite direction.
        return -tangent;
    }

    public static void RerailTrain(TrainCar trainCar, Vector3 forward)
    {
        var (closestTrack, point) = RailTrack.GetClosest(trainCar.transform.position);
        if (point == null)
            return;

        var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

        float t = closestTrack.curve.GetClosestT(rerailPos);
        var tangent = GetCurveTangent(closestTrack, t);

        void OnRerailed()
        {
            trainCar.brakeSystem.SetHandbrakePosition(0);
            trainCar.OnRerailed -= OnRerailed;
        }

        trainCar.OnRerailed += OnRerailed;

        if (trainCar.derailed)
            trainCar.Rerail(closestTrack, rerailPos, forward);
        else
            trainCar.SetTrack(closestTrack, rerailPos, forward);
    }

    // public static void RerailTrain(TrainCar trainCar, bool isReverse = false)
    // {
    //     var (closestTrack, point) = RailTrack.GetClosest(trainCar.transform.position);
    //     if (point == null)
    //         return;

    //     var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

    //     float t = closestTrack.curve.GetClosestT(rerailPos);
    //     var tangent = GetCurveTangent(closestTrack, t);

    //     if (isReverse)
    //         forward = -forward;

    //     void OnRerailed()
    //     {
    //         trainCar.brakeSystem.SetHandbrakePosition(0);
    //         trainCar.OnRerailed -= OnRerailed;
    //     }

    //     trainCar.OnRerailed += OnRerailed;

    //     if (trainCar.derailed)
    //         trainCar.Rerail(closestTrack, rerailPos, forward);
    //     else
    //         trainCar.SetTrack(closestTrack, rerailPos, forward);
    // }

    public static void EnableNoDerail()
    {
        var oldVal = Globals.G.GameParams.DerailStressThreshold;
        Logger.Log($"Enable no-derail ({oldVal}=>infinity)");
        Globals.G.GameParams.DerailStressThreshold = float.PositiveInfinity;
    }

    public static void DisableNoDerail()
    {
        var oldVal = Globals.G.GameParams.DerailStressThreshold;
        Logger.Log($"Disable no-derail ({oldVal}=>{Globals.G.GameParams.defaultStressThreshold})");
        Globals.G.GameParams.DerailStressThreshold = Globals.G.GameParams.defaultStressThreshold;
    }

    public static float? GetForwardSpeed(TrainCar car)
    {
        // TODO: cache
        var customComp = car.GetComponent<TrainCarCustomization>();

        if (customComp.HasPort(STDSimPort.WheelSpeedKMH))
            return customComp.ReadPort(STDSimPort.WheelSpeedKMH);

        return null;
    }

    public static void RepairTrain(TrainCar car)
    {
        // TODO: 
        // car.CarDamage.RepairCarEffectivePercentage(100f);

        var damageController = car.GetComponent<DamageController>();
        damageController.RepairAll();
    }

    public static void RerailTrainWithoutVelocity(TrainCar car, Vector3 forward)
    {
        var (closestTrack, point) = RailTrack.GetClosest(car.transform.position);

        var oldVelocity = car.rb.velocity;
        var oldAngularVelocity = car.rb.angularVelocity;

        void OnRerailed()
        {
            Logger.Log($"Rerail complete velocity({car.rb.velocity} => {oldVelocity}) angular({car.rb.angularVelocity} => {oldAngularVelocity})");

            car.rb.velocity = oldVelocity;
            car.rb.angularVelocity = oldAngularVelocity;

            car.brakeSystem.SetHandbrakePosition(0);
            car.OnRerailed -= OnRerailed;
        }

        car.OnRerailed += OnRerailed;

        var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

        if (car.derailed)
            car.Rerail(closestTrack, rerailPos, forward);
        else
            car.SetTrack(closestTrack, rerailPos, -forward);
    }

    public static bool IsCarUpright(TrainCar car, float maxTiltDegrees)
    {
        var up = car.transform.up;
        float angle = Vector3.Angle(up, Vector3.up);

        return angle <= maxTiltDegrees;
    }
}