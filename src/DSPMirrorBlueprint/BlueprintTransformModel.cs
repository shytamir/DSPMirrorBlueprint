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
        public int ModelIndex;
        public int AreaIndex;
        public int? InputObjectIndex;
        public int? OutputObjectIndex;
        public int OutputToSlot;
        public int InputFromSlot;
        public int OutputFromSlot;
        public int InputToSlot;
        public BlueprintVector3 Position;
        public BlueprintVector3 Position2;
        public BlueprintOrientation Orientation = new BlueprintOrientation();
        public BlueprintOrientation Orientation2 = new BlueprintOrientation();
    }

    internal sealed class BlueprintTransformSlotPose
    {
        public int Index;
        public BlueprintVector3 Position;
        public BlueprintOrientation Orientation = new BlueprintOrientation();
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
        public readonly Dictionary<int, List<BlueprintTransformSlotPose>> SlotPosesByModelIndex =
            new Dictionary<int, List<BlueprintTransformSlotPose>>();
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

            var buildingsByIndex = new Dictionary<int, BlueprintTransformBuilding>();
            foreach (BlueprintTransformBuilding building in model.Buildings)
                buildingsByIndex[building.Index] = building;

            foreach (BlueprintTransformBuilding building in model.Buildings)
            {
                RemapConnectionSlots(model, buildingsByIndex, building);
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

        private static void RemapConnectionSlots(
            BlueprintTransformModel model,
            Dictionary<int, BlueprintTransformBuilding> buildingsByIndex,
            BlueprintTransformBuilding building)
        {
            BlueprintTransformBuilding connected;
            if (building.OutputObjectIndex.HasValue &&
                buildingsByIndex.TryGetValue(building.OutputObjectIndex.Value, out connected))
            {
                building.OutputToSlot = ReflectSlotIndex(
                    model,
                    connected.ModelIndex,
                    building.OutputToSlot);
                building.OutputFromSlot = ReflectSlotIndex(
                    model,
                    building.ModelIndex,
                    building.OutputFromSlot);
            }

            if (building.InputObjectIndex.HasValue &&
                buildingsByIndex.TryGetValue(building.InputObjectIndex.Value, out connected))
            {
                building.InputFromSlot = ReflectSlotIndex(
                    model,
                    connected.ModelIndex,
                    building.InputFromSlot);
                building.InputToSlot = ReflectSlotIndex(
                    model,
                    building.ModelIndex,
                    building.InputToSlot);
            }
        }

        private static int ReflectSlotIndex(
            BlueprintTransformModel model,
            int modelIndex,
            int slotIndex)
        {
            if (slotIndex < 0) return slotIndex;

            List<BlueprintTransformSlotPose> poses;
            if (!model.SlotPosesByModelIndex.TryGetValue(modelIndex, out poses))
                return slotIndex;

            BlueprintTransformSlotPose source = null;
            foreach (BlueprintTransformSlotPose pose in poses)
                if (pose.Index == slotIndex) source = pose;
            if (source == null) return slotIndex;

            BlueprintVector3 targetPosition = source.Position;
            targetPosition.X = -targetPosition.X;
            BlueprintVector3 targetForward = source.Orientation.Forward;
            targetForward.X = -targetForward.X;
            BlueprintVector3 targetUp = source.Orientation.Up;
            targetUp.X = -targetUp.X;

            BlueprintTransformSlotPose best = null;
            float bestError = Single.MaxValue;
            foreach (BlueprintTransformSlotPose candidate in poses)
            {
                float error = DistanceSquared(candidate.Position, targetPosition) +
                    DistanceSquared(candidate.Orientation.Forward, targetForward) +
                    DistanceSquared(candidate.Orientation.Up, targetUp);
                if (error < bestError)
                {
                    best = candidate;
                    bestError = error;
                }
            }

            return best != null && bestError <= 0.0001f
                ? best.Index
                : slotIndex;
        }

        private static float DistanceSquared(
            BlueprintVector3 left,
            BlueprintVector3 right)
        {
            float x = left.X - right.X;
            float y = left.Y - right.Y;
            float z = left.Z - right.Z;
            return x * x + y * y + z * z;
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
