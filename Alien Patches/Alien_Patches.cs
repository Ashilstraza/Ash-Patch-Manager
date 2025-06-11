#if !RWPre1_4
#if !RWPre1_5
using AlienRace;
#endif
using Ash_Patch_Manager;

using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static AlienRace.AlienPartGenerator;

namespace Ashs_Alien_Patches
{
    /// <summary>
    /// Set of patches for all Alien races, provided by the Cutebold mod author
    /// </summary>
    public class Alien_Patches
    {
        private static readonly List<Action<bool>> hotReloadMethods = [];

        public static List<string> RaceList { get; private set; } = [];


        public Alien_Patches(Harmony harmony)
        {
            Type thisClass = typeof(Alien_Patches);

            string alienRaceID = "rimworld.erdelf.alien_race.main";

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef"))?.Transpilers?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                    method: AccessTools.Method(typeof(IncidentWorker_Disease), "CanAddHediffToAnyPartOfDef"),
                    patch: new HarmonyMethod(thisClass, nameof(Alien_FixBaby_Transpiler)),
                    patchMessage: "  Patching IncidentWorker_Disease.CanAddHediffToAnyPartOfDef"));
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible"))?.Transpilers?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                    method: AccessTools.Method(typeof(ITab_Pawn_Gear), "get_IsVisible"),
                    patch: new HarmonyMethod(thisClass, nameof(Alien_FixBaby_Transpiler)),
                    patchMessage: "  Patching ITab_Pawn_Gear.get_IsVisible"));
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor"))?.Transpilers?.Any(patch => !patch.owner.NullOrEmpty()) ?? true)
            {
                Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                    method: AccessTools.Method(typeof(Pawn_IdeoTracker), "get_CertaintyChangeFactor"),
                    patch: new HarmonyMethod(thisClass, nameof(Alien_CertaintyChangeFactor_Transpiler)),
                    patchMessage: "  Patching Pawn_IdeoTracker.get_CertaintyChangeFactor"));
            }

            if (!Harmony.GetPatchInfo(AccessTools.Method(typeof(HediffGiver), nameof(HediffGiver.TryApply)))?.Transpilers?.Any(patch => patch.owner == alienRaceID) ?? true)
            {
                Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                    method: AccessTools.Method(typeof(HediffGiver), nameof(HediffGiver.TryApply)),
                    patch: new HarmonyMethod(thisClass, nameof(Alien_FixBaby_Transpiler)),
                    patchMessage: "  Patching HediffGiver.TryApply"));
            }
#if !RWPre1_5
            /*Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                    method: AccessTools.Method(typeof(PlayDataLoader), nameof(PlayDataLoader.HotReloadDefs)),
                    patch: new HarmonyMethod(thisClass, nameof(Alien_HotReloadNotify)),
                    patchMessage: "  Adding HotReload support to HAR"));
            Ash_Patch_Manager.Ash_Patch_Manager.Register_Patch(new Wrapped_Patch(
                    method: AccessTools.Method(typeof(MapDrawer), nameof(MapDrawer.RegenerateEverythingNow)),
                    patch: new HarmonyMethod(thisClass, nameof(Alien_HotReload))));*/
#endif

#if !RWPre1_4
            /*Ash_Patch_Manager.Ash_Patch_Manager.Register_PatchSet(new Wrapped_PatchSet(
                    patchSet: typeof(DBHPatches),
                    args: [thisClass],
                    mod: "Dubwise.DubsBadHygiene"));*/
#endif

#if DEBUG && RW1_4   // Personal patches
            PersonalPatches(harmony, thisClass);
#endif
        }

#if !RWPre1_5

        /// <summary>If Rimworld is starting to hot reload.</summary>
        private static bool isHotReload = false;

        /// <summary>
        /// Sets isHotReload to true after the Dev function is called and before the reload happens.
        /// </summary>
        [PatchVersion(1), PatchType(PatchedTypes.Postfix)]
        public static void Alien_HotReloadNotify()
        {
            isHotReload = true;
        }

        /// <summary>
        /// Register a method to be called when the game is hot reloaded.
        /// </summary>
        /// <remarks>
        /// Expects a method with a single variable indicating that it is a hot reload, for example:
        /// <code>
        /// public static void BuildLists(bool hotReload)
        /// </code>
        /// </remarks>
        /// 
        /// <param name="method">The method to be called.</param>
        public static void Alien_HotReloadAddMethod(Action<bool> method)
        {
            hotReloadMethods.Add(method);
        }

        /// <summary>
        /// Hooks the map drawer's RegenerateEverythingNow() to make the call to rebuild HAR's lists if it is a hot reload.
        /// 
        /// Additionally, it also calls any registered methods.
        /// </summary>
        [PatchVersion(3), PatchType(PatchedTypes.Postfix)]
        public static void Alien_HotReload()
        {
            if (!isHotReload) return;
            // Peek to see if HAR is handling HotReloading properly; if it isn't then we do it ourselves.
            ThingDef_AlienRace alien = DefDatabase<ThingDef>.AllDefs.First(thingDef => thingDef is ThingDef_AlienRace) as ThingDef_AlienRace;

            if (!alien.alienRace.graphicPaths.body.GetSubGraphics().Any()) Alien_HotReloadLists();

            isHotReload = false;

            foreach (Action<bool> method in hotReloadMethods)
            {
                method(true);
            }
        }

        /// <summary>
        /// Rebuilds HAR's various restriction lists after a HotReload, handles everything except workGivers.
        /// </summary>
        private static void Alien_HotReloadLists()
        {
            AlienHarmony harmony = new(id: "rimworld.erdelf.alien_race.main");
            try
            {
                ThoughtSettings.thoughtRestrictionDict.Clear();
                RaceRestrictionSettings.apparelRestricted.Clear();
                RaceRestrictionSettings.weaponRestricted.Clear();
                RaceRestrictionSettings.buildingRestricted.Clear();//Read Only for whatever reason now
                RaceRestrictionSettings.recipeRestricted.Clear();
                RaceRestrictionSettings.plantRestricted.Clear();
                RaceRestrictionSettings.traitRestricted.Clear();
                RaceRestrictionSettings.foodRestricted.Clear();
                RaceRestrictionSettings.petRestricted.Clear();
                RaceRestrictionSettings.researchRestrictionDict.Clear();
                RaceRestrictionSettings.geneRestricted.Clear();
                RaceRestrictionSettings.geneRestrictedEndo.Clear();
                RaceRestrictionSettings.geneRestrictedXeno.Clear();
                RaceRestrictionSettings.xenotypeRestricted.Clear();
                RaceRestrictionSettings.reproductionRestricted.Clear();

                foreach (ThingDef_AlienRace ar in DefDatabase<ThingDef_AlienRace>.AllDefsListForReading)
                {
                    foreach (ThoughtDef thoughtDef in ar.alienRace.thoughtSettings.restrictedThoughts)
                    {
                        if (!ThoughtSettings.thoughtRestrictionDict.ContainsKey(thoughtDef))
                            ThoughtSettings.thoughtRestrictionDict.Add(thoughtDef, []);
                        ThoughtSettings.thoughtRestrictionDict[thoughtDef].Add(ar);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.apparelList)
                    {
                        RaceRestrictionSettings.apparelRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whiteApparelList.Add(thingDef);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.weaponList)
                    {
                        RaceRestrictionSettings.weaponRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whiteWeaponList.Add(thingDef);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.buildingList)
                    {
                        RaceRestrictionSettings.buildingRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whiteBuildingList.Add(thingDef);
                    }

                    foreach (RecipeDef recipeDef in ar.alienRace.raceRestriction.recipeList)
                    {
                        RaceRestrictionSettings.recipeRestricted.Add(recipeDef);
                        ar.alienRace.raceRestriction.whiteRecipeList.Add(recipeDef);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.plantList)
                    {
                        RaceRestrictionSettings.plantRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whitePlantList.Add(thingDef);
                    }

                    foreach (TraitDef traitDef in ar.alienRace.raceRestriction.traitList)
                    {
                        RaceRestrictionSettings.traitRestricted.Add(traitDef);
                        ar.alienRace.raceRestriction.whiteTraitList.Add(traitDef);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.foodList)
                    {
                        RaceRestrictionSettings.foodRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whiteFoodList.Add(thingDef);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.petList)
                    {
                        RaceRestrictionSettings.petRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whitePetList.Add(thingDef);
                    }

                    foreach (ResearchProjectDef projectDef in ar.alienRace.raceRestriction.researchList.SelectMany(selector: rl => rl?.projects))
                    {
                        if (!RaceRestrictionSettings.researchRestrictionDict.ContainsKey(projectDef))
                            RaceRestrictionSettings.researchRestrictionDict.Add(projectDef, []);
                        RaceRestrictionSettings.researchRestrictionDict[projectDef].Add(ar);
                    }

                    foreach (GeneDef geneDef in ar.alienRace.raceRestriction.geneList)
                    {
                        RaceRestrictionSettings.geneRestricted.Add(geneDef);
                        ar.alienRace.raceRestriction.whiteGeneList.Add(geneDef);
                    }

                    foreach (GeneDef geneDef in ar.alienRace.raceRestriction.geneListEndo)
                    {
                        RaceRestrictionSettings.geneRestrictedEndo.Add(geneDef);
                        ar.alienRace.raceRestriction.whiteGeneListEndo.Add(geneDef);
                    }

                    foreach (GeneDef geneDef in ar.alienRace.raceRestriction.geneListXeno)
                    {
                        RaceRestrictionSettings.geneRestrictedXeno.Add(geneDef);
                        ar.alienRace.raceRestriction.whiteGeneListXeno.Add(geneDef);
                    }

                    foreach (XenotypeDef xenotypeDef in ar.alienRace.raceRestriction.xenotypeList)
                    {
                        RaceRestrictionSettings.xenotypeRestricted.Add(xenotypeDef);
                        ar.alienRace.raceRestriction.whiteXenotypeList.Add(xenotypeDef);
                    }

                    foreach (ThingDef thingDef in ar.alienRace.raceRestriction.reproductionList)
                    {
                        RaceRestrictionSettings.reproductionRestricted.Add(thingDef);
                        ar.alienRace.raceRestriction.whiteReproductionList.Add(thingDef);
                    }

                    if (ar.alienRace.generalSettings.corpseCategory != ThingCategoryDefOf.CorpsesHumanlike)
                    {
                        ThingCategoryDefOf.CorpsesHumanlike.childThingDefs.Remove(ar.race.corpseDef);
                        if (ar.alienRace.generalSettings.corpseCategory != null)
                        {
                            ar.race.corpseDef.thingCategories = [ar.alienRace.generalSettings.corpseCategory];
                            ar.alienRace.generalSettings.corpseCategory.childThingDefs.Add(ar.race.corpseDef);
                            ar.alienRace.generalSettings.corpseCategory.ResolveReferences();
                        }
                        ThingCategoryDefOf.CorpsesHumanlike.ResolveReferences();
                    }

                    foreach (Pawn pawn in PawnsFinder.All_AliveOrDead)
                    {
                        AlienComp comp = pawn.TryGetComp<AlienComp>();
                        if (comp != null)
                        {
                            Traverse.Create(comp).Field("nodeProps").SetValue(null);
                            comp.CompRenderNodes();
                        }
                    }
                    ar.alienRace.generalSettings.alienPartGenerator.GenerateMeshsAndMeshPools();

                    if (ar.alienRace.generalSettings.humanRecipeImport && ar != ThingDefOf.Human)
                    {
                        (ar.recipes ??= []).AddRange(ThingDefOf.Human.recipes.Where(predicate: rd => !rd.targetsBodyPart ||
                                                                                                                                      rd.appliedOnFixedBodyParts.NullOrEmpty() ||
                                                                                                                                      rd.appliedOnFixedBodyParts.Any(predicate: bpd => ar.race.body.AllParts.Any(predicate: bpr => bpr.def == bpd))));

                        DefDatabase<RecipeDef>.AllDefsListForReading.ForEach(action: rd =>
                        {
                            if (rd.recipeUsers?.Contains(ThingDefOf.Human) ?? false)
                                rd.recipeUsers.Add(ar);
                            if (!rd.defaultIngredientFilter?.Allows(ThingDefOf.Meat_Human) ?? false)
                                rd.defaultIngredientFilter.SetAllow(ar.race.meatDef, allow: false);
                        });
                        ar.recipes.RemoveDuplicates();
                    }

                    // Unpatch 
                    ar.alienRace.raceRestriction?.workGiverList?.ForEach(action: wgd =>
                    {
                        if (wgd == null) return;

                        harmony.harmony.Unpatch(AccessTools.Method(wgd.giverClass, name: "JobOnThing"), HarmonyPatchType.All, "rimworld.erdelf.alien_race.main");
                        MethodInfo hasJobOnThingInfo = AccessTools.Method(wgd.giverClass, name: "HasJobOnThing");
                        if (hasJobOnThingInfo?.IsDeclaredMember() ?? false)
                            harmony.harmony.Unpatch(AccessTools.Method(wgd.giverClass, "HasJobOnThing"), HarmonyPatchType.All, "rimworld.erdelf.alien_race.main");
                    });

                    // Repatch
                    ar.alienRace.raceRestriction?.workGiverList?.ForEach(action: wgd =>
                    {
                        if (wgd == null) return;

                        harmony.Patch(AccessTools.Method(wgd.giverClass, name: "JobOnThing"),
                                      postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.GenericHasJobOnThingPostfix)));
                        MethodInfo hasJobOnThingInfo = AccessTools.Method(wgd.giverClass, name: "HasJobOnThing");
                        if (hasJobOnThingInfo?.IsDeclaredMember() ?? false)
                            harmony.Patch(hasJobOnThingInfo, postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(HarmonyPatches.GenericHasJobOnThingPostfix)));
                    });
                }

                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
                {
                    AnimalBodyAddons extension = def.GetModExtension<AnimalBodyAddons>();
                    if (extension != null)
                    {
                        extension.GenerateAddonData(def);
                        def.comps.Add(new CompProperties(typeof(AnimalComp)));
                    }
                }

                HairDefOf.Bald.styleTags.Add(item: "alienNoStyle");
                BeardDefOf.NoBeard.styleTags.Add(item: "alienNoStyle");
                TattooDefOf.NoTattoo_Body.styleTags.Add(item: "alienNoStyle");
                TattooDefOf.NoTattoo_Face.styleTags.Add(item: "alienNoStyle");
            }
            catch (Exception e)
            {
                Log.Error(string.Format($"Ash Patch Manager: Exception when trying Hot Reload, aborting custom HAR fix. If issues persist, please notify the author for the either the Argonian or Cutebold Race with the logs. Thanks!\n{e}"));
                return;
            }
        }
#endif

        /// <summary>
        /// Replaces instances of Pawn.ageTracker.CurLifeStage == LifeStageDefOf.HumanlikeBaby with Pawn.ageTracker.CurLifeStage.developmentalStage.Baby()
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>The fixed code.</returns>
        [PatchVersion(1), PatchType(PatchedTypes.Transpiler)]
        public static IEnumerable<CodeInstruction> Alien_FixBaby_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo humanLikeBaby = AccessTools.Field(typeof(LifeStageDefOf), "HumanlikeBaby");
            FieldInfo developmentalStage = AccessTools.Field(typeof(LifeStageDef), "developmentalStage");
            MethodInfo baby = AccessTools.Method(typeof(DevelopmentalStageExtensions), "Baby");

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;


            /*
             *  
             * Replaces == LifeStageDefOf.HumanlikeBaby with .developmentalStage.Baby()
             * 
             */
            List<CodeInstruction> babyFix =
            [
                new(OpCodes.Ldfld, developmentalStage), // Load developmentStage
                new(OpCodes.Call, baby), // Call Baby() on the loaded development stage
                new(OpCodes.Brtrue, null) // Branches if true, Operand label to be replaced on runtime
            ];

            for (int i = 0; i < instructionListCount; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i < instructionListCount && instructionList[i].Is(OpCodes.Ldsfld, humanLikeBaby))
                {
                    foreach (CodeInstruction codeInstruction in babyFix)
                    {
                        if (codeInstruction.opcode == OpCodes.Brtrue)
                        {
                            CodeInstruction tempInstruction = new(codeInstruction);
                            // Check if the branch instruction is branching when not equal
                            if (instructionList[i + 1].opcode == OpCodes.Bne_Un_S)
                            {
                                tempInstruction.opcode = OpCodes.Brfalse;
                            }

                            tempInstruction.operand = instructionList[i + 1].operand; // Grab branch label

                            yield return tempInstruction;
                        }
                        else yield return codeInstruction;
                    }

                    i += 2;
                    instruction = instructionList[i];
                }

                yield return instruction;
            }
        }

        /// <summary>
        /// Destructively replaces the SimpleCurve for the CertaintyChangeFactor
        /// </summary>
        /// <param name="instructions">The instructions we are messing with.</param>
        /// <param name="ilGenerator">The IDGenerator that allows us to create local variables and labels.</param>
        /// <returns>The code that is usable.</returns>
        [PatchVersion(1), PatchType(PatchedTypes.Transpiler)]
        public static IEnumerable<CodeInstruction> Alien_CertaintyChangeFactor_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo alienCertaintyChangeFactor = AccessTools.Method(typeof(Alien_Patches), nameof(Alien_Patches.CertaintyCurve));
            string x = Traverse.Create(typeof(Pawn_IdeoTracker)).Fields()[0];
            FieldInfo pawn = AccessTools.Field(typeof(Pawn_IdeoTracker), "pawn");

            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;

            /*
             * See drSpy decompile of PawnIdeo_Tracker.get_CertaintyChangeFactor() for variable references 
             */
            List<CodeInstruction> racialCertainty =
            [
                new(OpCodes.Ldarg_0), // Load this
                new(OpCodes.Ldfld, pawn), // Load this.pawn
                new(OpCodes.Call, alienCertaintyChangeFactor), // Call Alien_CertaintyChangeFactor(pawn)
                new CodeInstruction(OpCodes.Ret), // Returns the adjusted float from the previous call
            ];

            for (int i = 0; i < instructionListCount; i++)
            {
                CodeInstruction instruction = instructionList[i];

                if (i + 1 < instructionListCount && instructionList[i].opcode == OpCodes.Ldsfld)
                {

                    racialCertainty[0] = racialCertainty[0].MoveLabelsFrom(instructionList[i]);
                    foreach (CodeInstruction codeInstruction in racialCertainty)
                    {
                        yield return codeInstruction;
                    }
                    break;
                }

                yield return instruction;
            }
        }

        /// <summary>Racial ideology certainty curves</summary>
        private static readonly Dictionary<ThingDef, SimpleCurve> ideoCurves = [];
        /// <summary>Curve from ideoCurves that is going to be evaluated</summary>
        private static SimpleCurve certaintyCurveTemp;

        /// <summary>
        /// Creates a certainty curve for each race. Ideology does not matter, just the pawn's child and adult ages.
        /// </summary>
        /// <param name="pawn">Pawn to check the ideology certainty curve of.</param>
        /// <returns>Float between the values of 2 and 1 depending on how close to an adult the pawn is.</returns>
        public static float CertaintyCurve(Pawn pawn)
        {
            if (pawn == null) return 1f;

            if (ideoCurves.TryGetValue(pawn.def, out certaintyCurveTemp))
            {
                return certaintyCurveTemp.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            }

            float child = -1;
            float adult = -1;

            foreach (LifeStageAge lifeStage in pawn.RaceProps.lifeStageAges)
            {
                if (child == -1 && lifeStage.def.developmentalStage.Child()) child = lifeStage.minAge; //Just want the first lifeStage where a pawn is a child
                if (lifeStage.def.developmentalStage.Adult()) adult = lifeStage.minAge; //Want last life stage to match how Humans work. Does not take into account a life stage like "elder" if a custom race would have that.
            }

            //If for some reason a stage is not found, set the ages to be extremely low.
            if (child == -1) child = 0;
            if (adult < child) adult = child + 0.001f; //Add 0.001 to the child to prevent an inverted curve if the adult age is less than the child age.

            ideoCurves.Add(pawn.def,
                [
                    new CurvePoint(child, 2f),
                        new CurvePoint(adult, 1f)
                ]);

            return ideoCurves[pawn.def].Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
        }

        #region Personal Patches
#if DEBUG && RW1_4
        /// <summary>
        /// Personal Patches for various annoyances/bugs
        /// </summary>
        [PatchVersion(1)]
        private static void PersonalPatches(Harmony harmony, Type thisClass)
        {
            Ash_Patch_Manager.Ash_Patch_Manager.Register_PatchSet(new Wrapped_PatchSet(
                    patchSet: typeof(SomeThingsFloat),
                    mod: "Mlie.SomeThingsFloat",
                    args: [thisClass]));
            Ash_Patch_Manager.Ash_Patch_Manager.Register_PatchSet(new Wrapped_PatchSet(
                    patchSet: typeof(RFRumorHasIt),
                    mod: "Mlie.RFRumorHasIt",
                    args: [thisClass]));
            Ash_Patch_Manager.Ash_Patch_Manager.Register_PatchSet(new Wrapped_PatchSet(
                    patchSet: typeof(GZP),
                    mod: "babylettuce.growingzone",
                    args: [thisClass]));

            // faster baby feeding, maybe new mod?
            harmony.Patch(AccessTools.GetDeclaredMethods(typeof(JobDriver_BottleFeedBaby)).ElementAt(13), transpiler: new HarmonyMethod(thisClass, nameof(FasterFeeding_Transpiler)));
            harmony.Patch(AccessTools.Method(typeof(ChildcareUtility), "SuckleFromLactatingPawn"), transpiler: new HarmonyMethod(thisClass, nameof(FasterFeeding_Transpiler)));
        }

   // Personal patches
        [PatchVersion(1), PatchType(PatchedTypes.Transpiler)]
        public static IEnumerable<CodeInstruction> FasterFeeding_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> instructionList = instructions.ToList();
            int instructionListCount = instructionList.Count;

            for (int i = 0; i < instructionListCount; i++)
            {


                if (instructionList[i].Is(OpCodes.Ldc_R4, 5000f))
                {
                    instructionList[i].operand = 1250f;
                }
                yield return instructionList[i];
            }
        }
#endif
        #endregion
    }
}
#endif