#if DEBUG && RW1_4
using SomeThingsFloat;
using Rumor_Code;
using GrowingZonePlus;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.Linq;
using Verse;
#endif

#if !RWPre1_4
using Ash_Patch_Manager;
using DubsBadHygiene;
using HarmonyLib;
using System;

namespace Ashs_Alien_Patches
{
    /// <summary>
    /// Dub's Bad Hygene patch for handling non-human babies.
    /// </summary>
    public class DBHPatches
    {
        private static readonly int version = 1;

        public DBHPatches(Type alienPatches)
        {
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Transpiler,
                   method: AccessTools.Method(typeof(NeedsUtil), "ShouldHaveNeed"),
                   patch: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)),
                   version: version,
                   patchMessage: "  Patching DubsBadHygene.NeedsUtil.ShouldHaveNeed"));
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Transpiler,
                   method: AccessTools.Method(typeof(WorkGiver_washPatient), "ShouldBeWashed"),
                   patch: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)),
                   version: version,
                   patchMessage: "  Patching DubsBadHygene.WorkGiver_washPatient.ShouldBeWashed"));
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Transpiler,
                   method: AccessTools.Method(typeof(WorkGiver_washChild), "ShouldBeWashed"),
                   patch: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)),
                   version: version,
                   patchMessage: "  Patching DubsBadHygene.WorkGiver_washChild.ShouldBeWashed"));
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Transpiler,
                   method: AccessTools.Method(typeof(WorkGiver_washChild), "PotentialWorkThingsGlobal"),
                   patch: new HarmonyMethod(alienPatches, nameof(Alien_Patches.Alien_FixBaby_Transpiler)),
                   version: version,
                   patchMessage: "  Patching DubsBadHygene.WorkGiver_washChild.PotentialWorkThingsGlobal"));
        }
    }
    // Personal Patches
#if DEBUG && RW1_4
    internal class SomeThingsFloat
    {
        private static readonly int version = 1;

        public SomeThingsFloat(Type SomeThingsFloat)
        {
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Transpiler,
                   method: AccessTools.Method(typeof(FloatingThings_MapComponent), "updateListOfWaterCells"),
                   patch: new HarmonyMethod(SomeThingsFloat, nameof(QuickFix_SomeThingsFloat_Transpiler)),
                   version: version,
                   patchMessage: "  Patching WorkGiver_washChild.PotentialWorkThingsGlobal"));
        }

        public static IEnumerable<CodeInstruction> QuickFix_SomeThingsFloat_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo map = AccessTools.Field(typeof(MapComponent), "map");
            FieldInfo terrainGrid = AccessTools.Field(typeof(Map), "terrainGrid");
            FieldInfo underGrid = AccessTools.Field(typeof(TerrainGrid), "underGrid");
            FieldInfo topGrid = AccessTools.Field(typeof(TerrainGrid), "topGrid");

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;
            int n = 0;

            List<CodeInstruction> fix =
            [
                new(OpCodes.Ldarg_0),
                new(OpCodes.Ldfld, map),
                new(OpCodes.Ldfld, terrainGrid),
                new(OpCodes.Ldfld, underGrid),
                new(OpCodes.Ldloc_0),
                new(OpCodes.Ldelem_Ref),
                new(OpCodes.Brfalse, null)
            ];

            for (int i = 0; i < instructionListCount; i++)
            {
                yield return instructionList[i];

                if (i < instructionListCount && i > 4 && instructionList[i - 4].Is(OpCodes.Ldfld, topGrid))
                {
                    if (n == 1)
                    {
                        foreach (CodeInstruction instruction in fix)
                        {
                            if (instruction.opcode == OpCodes.Brfalse) instruction.operand = instructionList[i + 8].operand;

                            yield return instruction;
                        }
                    }

                    n++;
                }
            }
        }
    }

    internal class RFRumorHasIt
    {
        private static readonly int version = 1;

        public RFRumorHasIt(Type RFRumorHasIt)
        {
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Prefix,
                   method: AccessTools.Method(typeof(ThirdPartyManager), "FindCliques"),
                   patch: new HarmonyMethod(RFRumorHasIt, nameof(QuickFix_RFRumorHasIt)),
                   version: version,
                   patchMessage: "  Patching WorkGiver_washChild.PotentialWorkThingsGlobal"));
        }

        public static bool QuickFix_RFRumorHasIt()
        {
            return false;
        }
    }

    internal class GZP
    {
        private static readonly int version = 1;

        public GZP(Type gzp)
        {
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                   type: Wrapped_Patch.PatchedTypes.Postfix,
                   method: AccessTools.Method(typeof(Zone_GrowingPlus), "ExposeData"),
                   patch: new HarmonyMethod(gzp, nameof(GZP_ExposeData_Postfix)),
                   version: version,
                   patchMessage: "  Patching WorkGiver_washChild.PotentialWorkThingsGlobal"));
        }
        public static void GZP_ExposeData_Postfix(object __instance)
        {
            Zone_GrowingPlus growingZonePlus = __instance as Zone_GrowingPlus;
            var billStack = growingZonePlus.customBillStack;
            foreach (var bill in billStack)
            {
                var UID = Traverse.Create(bill).Field("zoneUniqueID");
                if (UID.GetValue() == null)
                {
                    UID.SetValue(growingZonePlus.UniqueID);
                }
                bill.zgp ??= growingZonePlus;

            }
        }
    }
#endif
}
#endif
