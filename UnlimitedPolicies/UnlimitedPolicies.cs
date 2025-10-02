using HarmonyLib;

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
            _harmony = new Harmony("com.hexofsteel.unlimitedpoliciesmod");
            _harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Player))]
    static class Patch_Player
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Player.AddPolicy))]
        static bool Patch_Pre_AddPolicy(Player __instance, Policy p_policy, bool p_sendRPC)
        {
            // This is the part of the function that removes any active policy from the same category
            // for (int i = 0; i < __instance.ListActivePolicies.Count; i++)
            //     if (__instance.ListActivePolicies[i].Category == p_policy.Category)
            //         __instance.RemovePolicy(__instance.ListActivePolicies[i].Type, p_sendRPC: false);

            // Here we just copy/pasted the base game content of the function so that it still runs the exact same

            __instance.ListActivePolicies.Add(p_policy);

            if (p_policy.Type == Policies.Type.Military_scientists_1)
            {
                __instance.NumberOfAvailableResearchPoints++;
            }
            else if (p_policy.Type == Policies.Type.Military_scientists_2)
            {
                __instance.NumberOfAvailableResearchPoints += 2;
            }

            // You will need to grab the PhotonUnityNetworking.dll from the game and place it in your mod project for this to compile
            if (p_sendRPC)
                MultiplayerManager.Instance.RunRPC("RPC_SyncPlayer", "O", new object[1] { __instance });

            return false;
        }
    }
}
