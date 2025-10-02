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
            __instance.ListActivePolicies.Add(p_policy);

            if (p_policy.Type == Policies.Type.Military_scientists_1)
            {
                __instance.NumberOfAvailableResearchPoints++;
            }
            else if (p_policy.Type == Policies.Type.Military_scientists_2)
            {
                __instance.NumberOfAvailableResearchPoints += 2;
            }

            if (p_sendRPC)
            {
                var multiplayerManager = Traverse.Create(typeof(MultiplayerManager)).Property("Instance").GetValue();
                if (multiplayerManager != null)
                {
                    Traverse.Create(multiplayerManager).Method("RunRPC", "RPC_SyncPlayer", "O", new object[1] { __instance }).GetValue();
                }
            }

            return false;
        }
    }
}