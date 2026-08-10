using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DSPMirrorBlueprint
{
    internal static class BlueprintRuntimeMirror
    {
        private static ManualLogSource logger;
        private static int handledFrame = -1;

        public static bool Install(
            Harmony harmony,
            ManualLogSource log,
            Func<bool> inputDiagnosticsEnabled,
            out string error)
        {
            logger = log;
            error = null;
            try
            {
                Type pasteToolType = AccessTools.TypeByName("BuildTool_BlueprintPaste");
                MethodInfo determinRotate = pasteToolType == null
                    ? null
                    : AccessTools.Method(pasteToolType, "DeterminRotate", Type.EmptyTypes);
                MethodInfo postfix = AccessTools.Method(
                    typeof(BlueprintRuntimeMirror),
                    "DeterminRotatePostfix");

                if (determinRotate == null || postfix == null)
                {
                    error = "the blueprint deployment rotation hook was not found.";
                    return false;
                }

                harmony.Patch(determinRotate, postfix: new HarmonyMethod(postfix));
                if (!GameInputBridge.Install(
                    harmony,
                    log,
                    inputDiagnosticsEnabled,
                    out error))
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        public static void Uninstall()
        {
            GameInputBridge.Uninstall();
            handledFrame = -1;
        }

        private static void DeterminRotatePostfix(object __instance, ref bool __result)
        {
            BlueprintMirrorAxis axis;
            if (!GameInputBridge.TryGetMirrorAxis(
                Time.frameCount,
                ref handledFrame,
                out axis))
            {
                return;
            }

            string error;
            if (BlueprintRuntimeAdapter.TryApply(__instance, axis, out error))
            {
                __result = true;
                GameInputBridge.LogMirrorResult(axis, true, null);
                return;
            }

            GameInputBridge.LogMirrorResult(axis, false, error);
            if (logger != null)
                logger.LogWarning("Blueprint mirror skipped: " + error);
        }
    }

    internal static class MirrorInputDecision
    {
        public static bool TrySelect(
            bool horizontalDown,
            bool verticalDown,
            int frame,
            ref int handledFrame,
            out BlueprintMirrorAxis axis)
        {
            axis = BlueprintMirrorAxis.Horizontal;
            if (handledFrame == frame || (!horizontalDown && !verticalDown))
                return false;

            handledFrame = frame;
            axis = verticalDown
                ? BlueprintMirrorAxis.Vertical
                : BlueprintMirrorAxis.Horizontal;
            return true;
        }
    }

    internal static class BlueprintRuntimeAdapter
    {
        public static bool TryApply(
            object pasteTool,
            BlueprintMirrorAxis axis,
            out string error)
        {
            error = null;
            try
            {
                object blueprint = GetRequiredField(pasteTool, "blueprint");
                if (blueprint == null)
                {
                    error = "the active paste tool has no blueprint data.";
                    return false;
                }

                Array buildingValues = GetRequiredField(blueprint, "buildings") as Array;
                Array areaValues = GetRequiredField(blueprint, "areas") as Array;
                object reformData = GetRequiredField(blueprint, "reformData");
                Array reformValues = reformData == null
                    ? null
                    : GetRequiredField(reformData, "rects") as Array;

                var model = new BlueprintTransformModel {
                    Width = ReadInt(blueprint, "dragBoxSize_x"),
                    Height = ReadInt(blueprint, "dragBoxSize_y"),
                    CursorOffsetX = ReadInt(blueprint, "cursorOffset_x"),
                    CursorOffsetY = ReadInt(blueprint, "cursorOffset_y")
                };

                ReadAreas(areaValues, model);
                ReadBuildings(buildingValues, model);
                ReadModelSlotPoses(model);
                ReadReforms(reformValues, model);
                BlueprintMirrorTransform.Apply(model, axis);
                WriteBuildings(buildingValues, model);
                WriteReforms(reformValues, model);
                SetField(blueprint, "cursorOffset_x", model.CursorOffsetX);
                SetField(blueprint, "cursorOffset_y", model.CursorOffsetY);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static void ReadAreas(Array values, BlueprintTransformModel model)
        {
            if (values == null) return;
            foreach (object value in values)
            {
                if (value == null) continue;
                model.Areas.Add(new BlueprintTransformArea {
                    Index = ReadInt(value, "index"),
                    ParentIndex = ReadInt(value, "parentIndex"),
                    TropicAnchor = ReadInt(value, "tropicAnchor"),
                    AreaSegments = ReadInt(value, "areaSegments"),
                    AnchorLocalOffsetX = ReadInt(value, "anchorLocalOffsetX"),
                    AnchorLocalOffsetY = ReadInt(value, "anchorLocalOffsetY"),
                    Width = ReadInt(value, "width"),
                    Height = ReadInt(value, "height")
                });
            }
        }

        private static void ReadBuildings(Array values, BlueprintTransformModel model)
        {
            if (values == null) return;
            foreach (object value in values)
            {
                if (value == null) continue;
                model.Buildings.Add(new BlueprintTransformBuilding {
                    Index = ReadInt(value, "index"),
                    ModelIndex = ReadInt(value, "modelIndex"),
                    AreaIndex = ReadInt(value, "areaIndex"),
                    InputObjectIndex = ReadConnectionIndex(
                        value,
                        "inputObj",
                        "tempInputObjIdx"),
                    OutputObjectIndex = ReadConnectionIndex(
                        value,
                        "outputObj",
                        "tempOutputObjIdx"),
                    OutputToSlot = ReadInt(value, "outputToSlot"),
                    InputFromSlot = ReadInt(value, "inputFromSlot"),
                    OutputFromSlot = ReadInt(value, "outputFromSlot"),
                    InputToSlot = ReadInt(value, "inputToSlot"),
                    Position = ReadPosition(value, String.Empty),
                    Position2 = ReadPosition(value, "2"),
                    Orientation = ReadOrientation(value, String.Empty),
                    Orientation2 = ReadOrientation(value, "2")
                });
            }
        }

        private static void ReadModelSlotPoses(BlueprintTransformModel model)
        {
            Type ldbType = AccessTools.TypeByName("LDB");
            object models = GetStaticMember(ldbType, "models", "_models");
            var modelIndices = new HashSet<int>();
            foreach (BlueprintTransformBuilding building in model.Buildings)
                modelIndices.Add(building.ModelIndex);

            foreach (int modelIndex in modelIndices)
            {
                object modelProto = InvokeSelect(models, modelIndex);
                object prefabDesc = GetFieldOrNull(modelProto, "prefabDesc");
                Array poseValues = GetFieldOrNull(prefabDesc, "slotPoses") as Array;
                if (poseValues == null || poseValues.Length == 0) continue;

                var poses = new List<BlueprintTransformSlotPose>();
                for (int i = 0; i < poseValues.Length; i++)
                {
                    Pose pose = (Pose)poseValues.GetValue(i);
                    poses.Add(new BlueprintTransformSlotPose {
                        Index = i,
                        Position = FromUnityVector(pose.position),
                        Orientation = OrientationFromRotation(pose.rotation)
                    });
                }
                model.SlotPosesByModelIndex[modelIndex] = poses;
            }
        }

        private static void ReadReforms(Array values, BlueprintTransformModel model)
        {
            if (values == null) return;
            foreach (object value in values)
            {
                model.Reforms.Add(new BlueprintTransformReform {
                    AreaIndex = ReadInt(value, "areaIndex"),
                    X = ReadInt(value, "x"),
                    Y = ReadInt(value, "y"),
                    Width = ReadInt(value, "w"),
                    Height = ReadInt(value, "h"),
                    Data = Convert.ToByte(GetRequiredField(value, "data"))
                });
            }
        }

        private static void WriteBuildings(Array values, BlueprintTransformModel model)
        {
            if (values == null) return;
            int modelIndex = 0;
            for (int i = 0; i < values.Length; i++)
            {
                object value = values.GetValue(i);
                if (value == null) continue;
                BlueprintTransformBuilding building = model.Buildings[modelIndex++];
                WritePosition(value, String.Empty, building.Position);
                WritePosition(value, "2", building.Position2);
                WriteOrientation(value, String.Empty, building.Orientation);
                WriteOrientation(value, "2", building.Orientation2);
                SetField(value, "outputToSlot", building.OutputToSlot);
                SetField(value, "inputFromSlot", building.InputFromSlot);
                SetField(value, "outputFromSlot", building.OutputFromSlot);
                SetField(value, "inputToSlot", building.InputToSlot);
            }
        }

        private static void WriteReforms(Array values, BlueprintTransformModel model)
        {
            if (values == null) return;
            for (int i = 0; i < values.Length; i++)
            {
                object value = values.GetValue(i);
                BlueprintTransformReform reform = model.Reforms[i];
                SetField(value, "x", reform.X);
                SetField(value, "y", reform.Y);
                values.SetValue(value, i);
            }
        }

        private static BlueprintVector3 ReadPosition(object building, string suffix)
        {
            return new BlueprintVector3(
                ReadFloat(building, "localOffset_x" + suffix),
                ReadFloat(building, "localOffset_y" + suffix),
                ReadFloat(building, "localOffset_z" + suffix));
        }

        private static void WritePosition(
            object building,
            string suffix,
            BlueprintVector3 position)
        {
            SetField(building, "localOffset_x" + suffix, position.X);
            SetField(building, "localOffset_y" + suffix, position.Y);
            SetField(building, "localOffset_z" + suffix, position.Z);
        }

        private static BlueprintOrientation ReadOrientation(
            object building,
            string suffix)
        {
            Quaternion rotation = Quaternion.Euler(
                ReadFloat(building, "pitch" + suffix),
                ReadFloat(building, "yaw" + suffix),
                ReadFloat(building, "tilt" + suffix));
            return OrientationFromRotation(rotation);
        }

        private static BlueprintOrientation OrientationFromRotation(
            Quaternion rotation)
        {
            return new BlueprintOrientation {
                Forward = FromUnityVector(rotation * Vector3.forward),
                Up = FromUnityVector(rotation * Vector3.up)
            };
        }

        private static int? ReadConnectionIndex(
            object building,
            string objectField,
            string temporaryIndexField)
        {
            object connected = GetFieldOrNull(building, objectField);
            if (connected != null)
                return ReadInt(connected, "index");

            int temporaryIndex = ReadInt(building, temporaryIndexField);
            return temporaryIndex >= 0 ? (int?)temporaryIndex : null;
        }

        private static void WriteOrientation(
            object building,
            string suffix,
            BlueprintOrientation orientation)
        {
            Vector3 forward = ToUnityVector(orientation.Forward);
            Vector3 up = ToUnityVector(orientation.Up);
            Vector3 euler = Quaternion.LookRotation(forward, up).eulerAngles;
            SetField(building, "pitch" + suffix, euler.x);
            SetField(building, "yaw" + suffix, euler.y);
            SetField(building, "tilt" + suffix, euler.z);
        }

        private static BlueprintVector3 FromUnityVector(Vector3 value)
        {
            return new BlueprintVector3(value.x, value.z, value.y);
        }

        private static Vector3 ToUnityVector(BlueprintVector3 value)
        {
            return new Vector3(value.X, value.Z, value.Y);
        }

        private static int ReadInt(object instance, string name)
        {
            return Convert.ToInt32(GetRequiredField(instance, name));
        }

        private static float ReadFloat(object instance, string name)
        {
            return Convert.ToSingle(GetRequiredField(instance, name));
        }

        private static object GetRequiredField(object instance, string name)
        {
            if (instance == null)
                throw new MissingFieldException("Cannot read '" + name + "' from a null instance.");

            FieldInfo field = FindField(instance.GetType(), name);
            if (field == null)
                throw new MissingFieldException(instance.GetType().FullName, name);
            return field.GetValue(instance);
        }

        private static object GetFieldOrNull(object instance, string name)
        {
            if (instance == null) return null;
            FieldInfo field = FindField(instance.GetType(), name);
            return field == null ? null : field.GetValue(instance);
        }

        private static object GetStaticMember(Type type, params string[] names)
        {
            if (type == null) return null;
            const BindingFlags flags = BindingFlags.Static |
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.FlattenHierarchy;
            foreach (string name in names)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null) return field.GetValue(null);
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null) return property.GetValue(null, null);
            }
            return null;
        }

        private static object InvokeSelect(object instance, int id)
        {
            if (instance == null) return null;
            MethodInfo method = instance.GetType().GetMethod(
                "Select",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int) },
                null);
            return method == null
                ? null
                : method.Invoke(instance, new object[] { id });
        }

        private static void SetField(object instance, string name, object value)
        {
            FieldInfo field = FindField(instance.GetType(), name);
            if (field == null)
                throw new MissingFieldException(instance.GetType().FullName, name);
            field.SetValue(instance, Convert.ChangeType(value, field.FieldType));
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                FieldInfo field = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) return field;
                type = type.BaseType;
            }
            return null;
        }
    }
}
