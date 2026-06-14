// ===============================
// File: TrenchMazeGenerator.Extensions.cs (helpers)
// ===============================
using UnityEngine;

namespace TrenchMaze
{
    public static class TrenchMazeGeneratorExtensions
    {
        // Expose widths for mutator randomization convenience
        public static int SurfaceParentWidth(this TrenchMazeGenerator gen) => Mathf.Max(3, gen.SurfaceWidth());
        public static int SurfaceParentHeight(this TrenchMazeGenerator gen) => Mathf.Max(3, gen.SurfaceHeight());

        public static int SurfaceWidth(this TrenchMazeGenerator gen) => gen.Surface != null ? gen.SurfaceWidthInternal() : gen.width;
        public static int SurfaceHeight(this TrenchMazeGenerator gen) => gen.Surface != null ? gen.SurfaceHeightInternal() : gen.height;

        // Internal width/height accessors (kept simple; GridGraph doesn't directly expose w/h)
        public static int SurfaceWidthInternal(this TrenchMazeGenerator gen) => gen.width;
        public static int SurfaceHeightInternal(this TrenchMazeGenerator gen) => gen.height;

        /// <summary>
        /// Rebuild only the surface visual geometry from the current graph without regenerating topology.
        /// </summary>
        public static void RebuildSurfaceOnly(this TrenchMazeGenerator gen)
        {
            if (gen.Surface == null) return;
            var surfField = typeof(TrenchMazeGenerator).GetField("surface", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var parentField = typeof(TrenchMazeGenerator).GetField("surfaceParent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var method = typeof(TrenchMazeGenerator).GetMethod("BuildLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var surf = surfField.GetValue(gen);
            var parent = parentField.GetValue(gen) as Transform;
            method.Invoke(gen, new object[] { surf, parent, 0f });
        }
    }
}
