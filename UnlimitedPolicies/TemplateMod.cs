using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace UnlimitedPoliciesMod
{
    public class UnlimitedPoliciesMod : GameModification
    {
        Harmony _harmony;

        public UnlimitedPoliciesMod(Mod p_mod) : base(p_mod) { }

        public override void OnModInitialization(Mod p_mod)
        {
            mod = p_mod;
            PatchGame();
        }

        public override void OnModUnloaded()
        {
            _harmony?.UnpatchAll(_harmony.Id);
        }

        void PatchGame()
        {
            _harmony = new Harmony("com.hexofsteel.unlimitedpolicies");
            _harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Player), "AddPolicy")]
    static class Patch_Player_AddPolicy
    {
        static bool Prefix(Player __instance, Policy p_policy, bool p_sendRPC)
        {
            if (__instance.HasPolicy(p_policy.Type))
                return false;

            if (p_policy.HighCommandPointsCost > __instance.HighCommandPoints)
                return false;

            List<Policy> policiesToRemove = new List<Policy>();

            __instance.HighCommandPoints -= p_policy.HighCommandPointsCost;

            __instance.ListActivePolicies.Add(p_policy);

            switch (p_policy.Type)
            {
                case Policies.Type.Military_scientists_1:
                    __instance.NumberOfAvailableResearchPoints++;
                    break;
                case Policies.Type.Military_scientists_2:
                    __instance.NumberOfAvailableResearchPoints += 2;
                    break;
            }

            if (p_sendRPC)
            {
                var multiplayerManager = Traverse.Create(typeof(MultiplayerManager)).Property("Instance").GetValue();
                if (multiplayerManager != null)
                {
                    Traverse.Create(multiplayerManager).Method("RunRPC", "RPC_SyncPlayer", "O", new object[1] { __instance }).GetValue();
                }
            }

            if (UIManager.instance != null)
            {
                UIManager.instance.RefreshManpower();
                UIManager.instance.RefreshIncomePerTurnUI();
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PolicySlotGO), "SetUpPolicySlot")]
    static class Patch_PolicySlotGO_SetUpPolicySlot
    {
        static void Postfix(PolicySlotGO __instance, Policy p_policy)
        {
            if (!TurnManager.currPlayer.HasPolicy(p_policy.Type) && 
                p_policy.HighCommandPointsCost <= TurnManager.currPlayer.HighCommandPoints)
            {
                __instance.button.interactable = true;
                __instance.representation_image.color = Color.white;
            }
        }
    }
}