using System;

namespace Ash_Patch_Manager
{
    /// <summary>
    /// The supported types of patches.
    /// </summary>
    public enum PatchedTypes : byte
    {
        Prefix,
        Postfix,
        Transpiler
    }

    /// <summary>
    /// Assigns a Patch Version to a Method for deciding which method to use as the patch.
    /// </summary>
    /// <param name="version">The version of the Method.</param>
    public class PatchVersion(int version) : Attribute
    {
        /// <summary>Version of the Patch</summary>
        public int Version = version;
    }

    /// <summary>
    /// Assigns a Patch Type to a Method for deciding how to patch
    /// </summary>
    /// <param name="type">The type of patch/</param>
    public class PatchType(PatchedTypes type) : Attribute
    {
        public PatchedTypes Type = type;
    }
}
