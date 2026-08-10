using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DSPMirrorBlueprint
{
    internal static class BlueprintRuntimeMirror
    {
        private static ManualLogSource logger;
        private static int handledFrame = -1;

        public static bool Install(Harmony harmony, ManualLogSource log, out string error)
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
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }
        }

        private static void DeterminRotatePostfix(object __instance, ref bool __result)
        {
            if (!Input.GetKeyDown(KeyCode.K) || handledFrame == Time.frameCount)
                return;

            handledFrame = Time.frameCount;
            bool shift = Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);
            BlueprintMirrorAxis axis = shift
                ? BlueprintMirrorAxis.Vertical
                : BlueprintMirrorAxis.Horizontal;

            string error;
            if (BlueprintRuntimeAdapter.TryApply(__instance, axis, out error))
            {
                __result = true;
                return;
            }

            if (logger != null)
                logger.LogWarning("Blueprint mirror skipped: " + error);
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
                    AreaIndex = ReadInt(value, "areaIndex"),
                    Position = ReadPosition(value, String.Empty),
                    Position2 = ReadPosition(value, "2"),
                    Orientation = ReadOrientation(value, String.Empty),
                    Orientation2 = ReadOrientation(value, "2")
                });
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
            return new BlueprintOrientation {
                Forward = FromUnityVector(rotation * Vector3.forward),
                Up = FromUnityVector(rotation * Vector3.up)
            };
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
