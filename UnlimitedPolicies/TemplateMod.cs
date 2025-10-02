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
            _harmony = new Harmony("com.hexofsteel.doublepolicypercategory");
            _harmony.PatchAll();
        }
    }

    public static class PolicySelectionState
    {
        public static bool IsProcessingPolicy = false;
    }

    [HarmonyPatch(typeof(PoliciesGO), "ClickOnPolicy")]
    static class Patch_PoliciesGO_ClickOnPolicy_Final
    {
        static bool Prefix(PoliciesGO __instance, PolicySlotGO p_policySlotGO)
        {
            if (PolicySelectionState.IsProcessingPolicy)
                return true;

            PolicySelectionState.IsProcessingPolicy = true;

            try
            {
                SoundManager.instance.UI_Source.PlayOneShot(SoundManager.GetUISound("Click"));
                
                if (TurnManager.currPlayer.HasPolicy(p_policySlotGO.policy.Type))
                {
                    Traverse.Create(__instance).Field("_confirmationPolicy").SetValue(p_policySlotGO.policy);
                    
                    UIManager.ShowConfirmationWindow(
                        LocalizationManager.Translate("info-confirmation remove policy") + p_policySlotGO.policy.Name + " ?", 
                        () => OnConfirmRemovePolicy(__instance, p_policySlotGO.policy)
                    );
                    return false;
                }

                if (p_policySlotGO.policy.HighCommandPointsCost > TurnManager.currPlayer.HighCommandPoints)
                {
                    UIManager.ShowMessage(LocalizationManager.Translate("error-not enough hq points"));
                    return false;
                }

                AddPolicyDirectly(TurnManager.currPlayer, p_policySlotGO.policy, true);

                RefreshPoliciesUI(__instance);
                
                if (p_policySlotGO.policy.Type == Policies.Type.Forced_labor)
                {
                    SteamManager.UnlockAchievement("Tyrant");
                }
                if (p_policySlotGO.policy.Type == Policies.Type.Conscription3)
                {
                    SteamManager.UnlockAchievement("Scraping the barrel");
                }
                
                TutorialManager.PerformAction(TutorialActions.PICK_POLICY);

                return false;
            }
            finally
            {
                PolicySelectionState.IsProcessingPolicy = false;
            }
        }

        static void OnConfirmRemovePolicy(PoliciesGO instance, Policy policy)
        {
            RemovePolicyDirectly(TurnManager.currPlayer, policy, true);
            RefreshPoliciesUI(instance);
        }

        static void AddPolicyDirectly(Player player, Policy policy, bool sendRPC)
        {
            player.HighCommandPoints -= policy.HighCommandPointsCost;

            bool policyExists = false;
            foreach (var activePolicy in player.ListActivePolicies)
            {
                if (activePolicy.Type == policy.Type)
                {
                    policyExists = true;
                    break;
                }
            }

            if (!policyExists)
            {
                player.ListActivePolicies.Add(policy);
                
                if (policy.Type == Policies.Type.Military_scientists_1)
                {
                    player.NumberOfAvailableResearchPoints++;
                }
                else if (policy.Type == Policies.Type.Military_scientists_2)
                {
                    player.NumberOfAvailableResearchPoints += 2;
                }
            }

            if (sendRPC)
            {
                var multiplayerManager = Traverse.Create(typeof(MultiplayerManager)).Property("Instance").GetValue();
                if (multiplayerManager != null)
                {
                    Traverse.Create(multiplayerManager).Method("RunRPC", "RPC_SyncPlayer", "O", new object[1] { player }).GetValue();
                }
            }
        }

        static void RemovePolicyDirectly(Player player, Policy policy, bool sendRPC)
        {
            for (int i = 0; i < player.ListActivePolicies.Count; i++)
            {
                if (player.ListActivePolicies[i].Type == policy.Type)
                {
                    switch (policy.Type)
                    {
                        case Policies.Type.Military_scientists_1:
                            player.NumberOfAvailableResearchPoints--;
                            break;
                        case Policies.Type.Military_scientists_2:
                            player.NumberOfAvailableResearchPoints -= 2;
                            break;
                    }

                    if (player.HasPolicy(Policies.Type.Strategic_Flexibility))
                    {
                        player.AddHQPoints((byte)(policy.HighCommandPointsCost * 0.3f), false);
                    }

                    player.ListActivePolicies.RemoveAt(i);
                    break;
                }
            }

            if (sendRPC)
            {
                var multiplayerManager = Traverse.Create(typeof(MultiplayerManager)).Property("Instance").GetValue();
                if (multiplayerManager != null)
                {
                    Traverse.Create(multiplayerManager).Method("RunRPC", "RPC_SyncPlayer", "O", new object[1] { player }).GetValue();
                }
            }
        }

        static void RefreshPoliciesUI(PoliciesGO instance)
        {
            Traverse.Create(instance).Method("RefreshPolicies").GetValue();
            Traverse.Create(instance).Method("RefreshHighCommandpoints").GetValue();
            UIManager.instance.RefreshManpower();
            UIManager.instance.RefreshIncomePerTurnUI();
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

    [HarmonyPatch(typeof(Player), "AddPolicy")]
    static class Patch_Player_AddPolicy_Block
    {
        static bool Prefix(Player __instance, Policy p_policy, bool p_sendRPC)
        {
            if (PolicySelectionState.IsProcessingPolicy)
                return false;

            var stackTrace = new System.Diagnostics.StackTrace();
            foreach (var frame in stackTrace.GetFrames())
            {
                var method = frame.GetMethod();
                if (method.Name == "ClickOnPolicy" && method.DeclaringType == typeof(PoliciesGO))
                {
                    return false;
                }
            }

            return true;
        }
    }
}