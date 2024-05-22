using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Ash_Patch_Manager
{
    #region Wrapped Patch
    /// <summary>
    /// A conveniently wrapped up patch.
    /// </summary>
    /// <param name="type">The type of patch it is (Prefix, Postfix, or Transpiler)</param>
    /// <param name="method">The method to be patched</param>
    /// <param name="patch">The patch that is to be applied.</param>
    /// <param name="patchMessage">What, if anything, should be added to the patch log.</param>
    public class Wrapped_Patch(MethodBase method, HarmonyMethod patch, string patchMessage = "")
    {
        /// <summary>The type of patch this is.</summary>
        public PatchedTypes PatchType => (Attribute.GetCustomAttribute(patch.method, typeof(PatchType)) as PatchType).Type;
        /// <summary>The method this is patching.</summary>
        public MethodBase MethodBase => method;
        /// <summary>The patch that is being applied.</summary>
        public HarmonyMethod HarmonyMethod => patch;
        /// <summary>The version of patch this is.</summary>
        public Wrapped_Patch_Version Version { get; } = new Wrapped_Patch_Version(method, (Attribute.GetCustomAttribute(patch.method, typeof(PatchType)) as PatchType).Type, (Attribute.GetCustomAttribute(patch.method, typeof(PatchVersion)) as PatchVersion).Version);
        /// <summary>The message we should add to the patch log.</summary>
        public string PatchMessage => patchMessage;
    }

    /// <summary>
    /// A dedicated version for the patch.
    /// </summary>
    /// <param name="method">The method that is being patched.</param>
    /// <param name="type">The type of patch this is.</param>
    /// <param name="version">The version of patch.</param>
    public class Wrapped_Patch_Version(MethodBase method, PatchedTypes type, int version)
    {
        /// <summary>The version number.</summary>
        public int Version { get { return version; } }
        /// <summary>The type of patch.</summary>
        public PatchedTypes Type => type;
        /// <summary>The method the patch applies to.</summary>
        public MethodBase Method => method;

        public override string ToString()
        {
            return $"{type} patch, version {version}, for {method}";
        }

        public override bool Equals(object obj)
        {
            if (obj is Wrapped_Patch_Version o)
            {
                return version == o.Version && type == o.Type && method.Equals(o.Method);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Compares two Wrapped Patches
    /// </summary>
    public class Wrapped_Patch_Comparer : IComparer<Wrapped_Patch_Version>
    {
        int IComparer<Wrapped_Patch_Version>.Compare(Wrapped_Patch_Version x, Wrapped_Patch_Version y)
        {
            int result = TypeCompare(x, y);
            if (result == 0) result = VersionCompare(x, y);
            if (result == 0) result = MethodCompare(x, y);
            return result;
        }

        /// <summary>
        /// Compares two Wrapped Patches on the basis of their type
        /// </summary>
        /// <param name="x">The first patch to compare.</param>
        /// <param name="y">The second patch to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y:
        ///- If greater than 0, x is less than y.
        ///- If 0, x equals y.
        ///- If less than 0, x is greater than y.</returns>
        int TypeCompare(Wrapped_Patch_Version x, Wrapped_Patch_Version y)
        {
            return y.Type.CompareTo(x.Type);
        }

        /// <summary>
        /// Compares two Wrapped Patches on the basis of their version
        /// </summary>
        /// <param name="x">The first patch to compare.</param>
        /// <param name="y">The second patch to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y:
        ///- If greater than 0, x is less than y.
        ///- If 0, x equals y.
        ///- If less than 0, x is greater than y.</returns>
        int VersionCompare(Wrapped_Patch_Version x, Wrapped_Patch_Version y)
        {
            return y.Version.CompareTo(x.Version);
        }
        /// <summary>
        /// Compares two Wrapped Patches on the basis of their method name
        /// </summary>
        /// <param name="x">The first patch to compare.</param>
        /// <param name="y">The second patch to compare.</param>
        /// <returns>A signed integer that indicates the relative values of x and y:
        ///- If less than 0, x is less than y.
        ///- If 0, x equals y.
        ///- If greater than 0, x is greater than y.</returns>
        int MethodCompare(Wrapped_Patch_Version x, Wrapped_Patch_Version y)
        {
            return x.Method.Name.CompareTo(y.Method.Name);
        }
    }
    #endregion

    #region Wrapped Patch Set
    /// <summary>
    /// A conveniantly wrapped up set of patches.
    /// </summary>
    /// <param name="patchSet">The class containing a set of patches</param>
    /// <param name="mod">The mod the patches are for</param>
    public class Wrapped_PatchSet(Type patchSet, string mod, object[] args)
    {
        public Type PatchSet => patchSet;
        public string Mod => mod;
        public Wrapped_PatchSet_Version Version => new(mod, (Attribute.GetCustomAttribute(patchSet, typeof(PatchVersion)) as PatchVersion).Version);

        public object[] Args => args;
    }

    /// <summary>
    /// A dedicated version for the patch set.
    /// </summary>
    /// <param name="mod">The mod that the patch set is affecting.</param>
    /// <param name="version">The version of the patch set.</param>
    public class Wrapped_PatchSet_Version(string mod, int version)
    {
        public int Version { get { return version; } }
        public string Mod => mod;
    }

    /// <summary>
    /// Compares two Wrapped Patche Sets
    /// </summary>
    public class Wrapped_PatchSet_Comparer : IComparer<Wrapped_PatchSet_Version>
    {
        int IComparer<Wrapped_PatchSet_Version>.Compare(Wrapped_PatchSet_Version x, Wrapped_PatchSet_Version y)
        {
            return x.Version.CompareTo(y.Version);
        }
    }
    #endregion
}
