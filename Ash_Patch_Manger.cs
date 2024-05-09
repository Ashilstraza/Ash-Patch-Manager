using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace Ash_Patch_Manager
{
    /// <summary>
    /// Handles patching methods that have shared patches across several different mods.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Ash_Patch_Manager
    {
        /// <summary>List of the various patches to apply.</summary>
        private static readonly SortedDictionary<Wrapped_Patch_Version, Wrapped_Patch> patches = new(new Wrapped_Patch_Comparer());
        /// <summary>List of the various patch sets to apply</summary>
        private static readonly SortedDictionary<Wrapped_PatchSet_Version, Wrapped_PatchSet> modPatchSets = new(new Wrapped_PatchSet_Comparer());
        /// <summary>List of the varouos methods to patch</summary>
        private static readonly List<MethodBase> patchedMethods = [];

        private static readonly List<string> patchedMods = [];

        /// <summary>Ash Patch Manager Harmony ID</summary>
        public static readonly string HarmonyID = "rimworld.ashilstraza.patchmanager";
        /// <summary>Reference to harmony</summary>
        private static readonly Harmony harmony = new(HarmonyID);

        /// <summary>
        /// Main constructor that applies the patches after everything has loaded.
        /// </summary>
        static Ash_Patch_Manager()
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                string greetString = "Ashilstraza's Alien Patches:\n";

                StringBuilder stringBuilder = new(greetString);

                try
                {
                    foreach (string mod in patchedMods)
                    {
                        var patchSet = modPatchSets.First(patchSet => patchSet.Value.Mod == mod).Value;
                        Activator.CreateInstance(type: patchSet.PatchSet, args: patchSet.Args);
                    }
                    foreach (MethodBase method in patchedMethods)
                    {
                        
                        Wrapped_Patch prefix = patches.FirstOrDefault(patch => patch.Value.MethodBase == method && patch.Value.PatchType == Wrapped_Patch.PatchedTypes.Prefix).Value;
                        Wrapped_Patch postfix = patches.FirstOrDefault(patch => patch.Value.MethodBase == method && patch.Value.PatchType == Wrapped_Patch.PatchedTypes.Postfix).Value;
                        Wrapped_Patch transpiler = patches.FirstOrDefault(patch => patch.Value.MethodBase == method && patch.Value.PatchType == Wrapped_Patch.PatchedTypes.Transpiler).Value;
                        string message = $"{(prefix != null ? prefix.PatchMessage + "\n" : "")}{(postfix != null ? postfix.PatchMessage + "\n" : "")}{(transpiler != null ? transpiler.PatchMessage + "\n" : "")}";
                        if(!message.TrimEndNewlines().Equals("")) stringBuilder.Append(message);
                        harmony.Patch(method,
                            prefix: prefix?.HarmonyMethod,
                            postfix: postfix?.HarmonyMethod,
                            transpiler: transpiler?.HarmonyMethod);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"{HarmonyID}: Exception when trying to apply Alien Patches. Please notify the author for the cutebold/argonian mod with the logs. Thanks!\n{e}");
                    stringBuilder.AppendLine("Exception Thrown, Aborting Patching");
                }   
                finally
                {
                    if (!stringBuilder.ToString().Equals(greetString)) Log.Message(stringBuilder.ToString().TrimEndNewlines());
                }
            });
        }

        /// <summary>
        /// Register a patch to be applied.
        /// </summary>
        /// <param name="patch">The patch to be applied</param>
        public static void Register_Patch(Wrapped_Patch patch)
        {
            if (!patches.ContainsKey(patch.Version))
            {
                patches.Add(patch.Version, patch);
                patchedMethods.Add(patch.MethodBase);
            }
        }

        /// <summary>
        /// Registers a set of mod patches to be applied.
        /// </summary>
        /// <param name="patchSet">The patches to be applied, contained within a dedicated method.</param>
        public static void Register_PatchSet(Wrapped_PatchSet patchSet)
        {
            if (ModLister.GetActiveModWithIdentifier(patchSet.Mod) != null && !modPatchSets.ContainsKey(patchSet.Version))
            {
                modPatchSets.Add(patchSet.Version, patchSet);
                patchedMods.Add(patchSet.Mod);
            }
        }
    }
}
