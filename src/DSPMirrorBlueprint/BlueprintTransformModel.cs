using System;
using System.Collections.Generic;

namespace DSPMirrorBlueprint
{
    internal enum BlueprintMirrorAxis
    {
        Horizontal,
        Vertical
    }

    internal struct BlueprintVector3
    {
        public BlueprintVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X;
        public float Y;
        public float Z;
    }

    internal sealed class BlueprintOrientation
    {
        public BlueprintVector3 Forward;
        public BlueprintVector3 Up;
    }

    internal sealed class BlueprintTransformBuilding
    {
        public int Index;
        public int AreaIndex;
        public int? InputObjectIndex;
        public int? OutputObjectIndex;
        public BlueprintVector3 Position;
        public BlueprintVector3 Position2;
        public BlueprintOrientation Orientation = new BlueprintOrientation();
        public BlueprintOrientation Orientation2 = new BlueprintOrientation();
    }

    internal sealed class BlueprintTransformReform
    {
        public int AreaIndex;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public byte Data;
    }

    internal sealed class BlueprintTransformArea
    {
        public int Index;
        public int ParentIndex;
        public int TropicAnchor;
        public int AreaSegments;
        public int AnchorLocalOffsetX;
        public int AnchorLocalOffsetY;
        public int Width;
        public int Height;
    }

    internal sealed class BlueprintTransformModel
    {
        public int Width;
        public int Height;
        public int CursorOffsetX;
        public int CursorOffsetY;
        public readonly List<BlueprintTransformArea> Areas =
            new List<BlueprintTransformArea>();
        public readonly List<BlueprintTransformBuilding> Buildings =
            new List<BlueprintTransformBuilding>();
        public readonly List<BlueprintTransformReform> Reforms =
            new List<BlueprintTransformReform>();
    }

    internal static class BlueprintMirrorTransform
    {
        public static void Apply(
            BlueprintTransformModel model,
            BlueprintMirrorAxis axis)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (model.Width <= 0 || model.Height <= 0)
                throw new ArgumentOutOfRangeException(
                    "model",
                    "Blueprint transform bounds must be positive.");

            foreach (BlueprintTransformBuilding building in model.Buildings)
            {
                building.Position = ReflectPosition(
                    building.Position,
                    model.Width,
                    model.Height,
                    axis);
                building.Position2 = ReflectPosition(
                    building.Position2,
                    model.Width,
                    model.Height,
                    axis);
                ReflectOrientation(building.Orientation, axis);
                ReflectOrientation(building.Orientation2, axis);
            }

            foreach (BlueprintTransformReform reform in model.Reforms)
            {
                if (axis == BlueprintMirrorAxis.Vertical)
                    reform.X = model.Width - reform.X - reform.Width;
                else
                    reform.Y = model.Height - reform.Y - reform.Height;
            }

            if (axis == BlueprintMirrorAxis.Vertical)
                model.CursorOffsetX = model.Width - 1 - model.CursorOffsetX;
            else
                model.CursorOffsetY = model.Height - 1 - model.CursorOffsetY;
        }

        private static BlueprintVector3 ReflectPosition(
            BlueprintVector3 position,
            int width,
            int height,
            BlueprintMirrorAxis axis)
        {
            if (axis == BlueprintMirrorAxis.Vertical)
                position.X = width - 1f - position.X;
            else
                position.Y = height - 1f - position.Y;
            return position;
        }

        private static void ReflectOrientation(
            BlueprintOrientation orientation,
            BlueprintMirrorAxis axis)
        {
            if (orientation == null) return;

            if (axis == BlueprintMirrorAxis.Vertical)
            {
                orientation.Forward.X = -orientation.Forward.X;
                orientation.Up.X = -orientation.Up.X;
            }
            else
            {
                orientation.Forward.Y = -orientation.Forward.Y;
                orientation.Up.Y = -orientation.Up.Y;
            }
        }
    }
}
