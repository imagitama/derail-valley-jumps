using UnityEngine;
using System;

namespace DerailValleyJumps;

public class UpdateDriver : MonoBehaviour
{
    public Action? OnFrame;
    public Action? OnLateFrame;

    public void Start()
    {
        Main.ModEntry.Logger.Log($"UpdateDriver started");
    }

    public void Update()
    {
        try
        {
            OnFrame?.Invoke();
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Log($"UpdateDriver failed: {ex}");
        }
    }

    public void LateUpdate()
    {
        try
        {
            OnLateFrame?.Invoke();
        }
        catch (Exception ex)
        {
            Main.ModEntry.Logger.Log($"UpdateDriver failed: {ex}");
        }
    }

    public void OnDisable()
    {
        Main.ModEntry.Logger.Log($"UpdateDriver disabled");
    }

    public void OnDestroy()
    {
        Main.ModEntry.Logger.Log($"UpdateDriver destroyed");
    }
}
