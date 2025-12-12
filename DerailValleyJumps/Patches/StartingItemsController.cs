using DerailValleyJumps;
using HarmonyLib;
using UnityModManagerNet;

namespace DerailValleyJumps;

[HarmonyPatch(typeof(StartingItemsController), nameof(StartingItemsController.AddStartingItems))]
internal class StartingItemsControllerPatch
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static void Postfix(
        StartingItemsController __instance,
        SaveGameData saveGameData,
        bool firstTime
    )
    {
        Logger.Log("StartingItemsController.AddStartingItems Postfix");
        Main.jumpManager?.Start();
    }
}
