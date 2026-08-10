using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace DSPMirrorBlueprint
{
    internal static class BlueprintGeometryDumper
    {
        private const string SchemaVersion = "1.1";
        private const int MaximumSnapshotBytes = 4 * 1024 * 1024;

        public static bool TryDump(out string path, out string error)
        {
            path = null;
            error = null;

            try
            {
                object blueprint;
                if (!TryGetActiveBlueprint(out blueprint, out error))
                    return false;

                Dictionary<string, object> snapshot = BuildSnapshot(blueprint);
                string json = DiagnosticJson.Stringify(snapshot);
                int byteCount = Encoding.UTF8.GetByteCount(json);
                if (byteCount > MaximumSnapshotBytes)
                {
                    error = "geometry snapshot exceeded " +
                        MaximumSnapshotBytes.ToString(CultureInfo.InvariantCulture) +
                        " bytes; use a smaller diagnostic blueprint.";
                    return false;
                }

                string outputDirectory = Path.Combine(
                    Paths.BepInExRootPath,
                    "DSP-Mirror-Blueprint",
                    "Diagnostics");
                Directory.CreateDirectory(outputDirectory);
                string stamp = DateTime.UtcNow.ToString(
                    "yyyyMMdd-HHmmssfff",
                    CultureInfo.InvariantCulture);
                path = Path.Combine(
                    outputDirectory,
                    "DSP-Mirror-Blueprint-Geometry-" + stamp + ".json");
                File.WriteAllText(path, json, new UTF8Encoding(false));
                return true;
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                path = null;
                return false;
            }
        }

        private static bool TryGetActiveBlueprint(
            out object blueprint,
            out string error)
        {
            blueprint = null;
            error = null;

            Type gameMainType = FindType("GameMain");
            if (gameMainType == null)
            {
                error = "GameMain was not found.";
                return false;
            }

            object player = GetStatic(gameMainType, "mainPlayer");
            object controller = GetMember(player, "controller");
            object actionBuild = GetMember(controller, "actionBuild");
            if (actionBuild == null)
            {
                error = "no active player build action was found.";
                return false;
            }

            object mode = GetMember(actionBuild, "blueprintMode");
            if (!String.Equals(
                Convert.ToString(mode, CultureInfo.InvariantCulture),
                "Paste",
                StringComparison.OrdinalIgnoreCase))
            {
                error = "open a blueprint for deployment before pressing the dump key.";
                return false;
            }

            object pasteTool = GetMember(actionBuild, "blueprintPasteTool");
            blueprint = GetMember(pasteTool, "blueprint");
            if (blueprint == null)
            {
                error = "the active blueprint paste tool has no blueprint data.";
                return false;
            }

            return true;
        }

        private static Dictionary<string, object> BuildSnapshot(object blueprint)
        {
            object blueprintBuildings = GetMember(blueprint, "buildings");
            List<object> areas = ExportSequence(
                GetMember(blueprint, "areas"),
                ExportArea);
            List<object> buildings = ExportSequence(
                blueprintBuildings,
                ExportBuilding);
            List<object> modelSlotPoseSets = ExportModelSlotPoseSets(
                blueprintBuildings);
            object reformData = GetMember(blueprint, "reformData");
            List<object> reforms = ExportSequence(
                GetMember(reformData, "rects"),
                ExportReform);

            var geometry = new Dictionary<string, object> {
                { "cursorOffsetX", GetMember(blueprint, "cursorOffset_x") },
                { "cursorOffsetY", GetMember(blueprint, "cursorOffset_y") },
                { "cursorTargetArea", GetMember(blueprint, "cursorTargetArea") },
                { "dragBoxSizeX", GetMember(blueprint, "dragBoxSize_x") },
                { "dragBoxSizeY", GetMember(blueprint, "dragBoxSize_y") },
                { "primaryAreaIndex", GetMember(blueprint, "primaryAreaIdx") },
                { "areaCount", areas.Count },
                { "buildingCount", buildings.Count },
                { "reformRectCount", reforms.Count },
                { "modelSlotPoseSetCount", modelSlotPoseSets.Count },
                { "areas", areas },
                { "buildings", buildings },
                { "reformRects", reforms },
                { "modelSlotPoseSets", modelSlotPoseSets }
            };

            return new Dictionary<string, object> {
                { "schemaVersion", SchemaVersion },
                { "pluginVersion", Plugin.PluginVersion },
                { "capturedAtUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) },
                { "assemblyMvid", blueprint.GetType().Assembly.ManifestModule.ModuleVersionId.ToString() },
                { "geometry", geometry }
            };
        }

        private static object ExportArea(object area)
        {
            return new Dictionary<string, object> {
                { "index", GetMember(area, "index") },
                { "parentIndex", GetMember(area, "parentIndex") },
                { "tropicAnchor", GetMember(area, "tropicAnchor") },
                { "areaSegments", GetMember(area, "areaSegments") },
                { "anchorLocalOffsetX", GetMember(area, "anchorLocalOffsetX") },
                { "anchorLocalOffsetY", GetMember(area, "anchorLocalOffsetY") },
                { "width", GetMember(area, "width") },
                { "height", GetMember(area, "height") }
            };
        }

        private static object ExportBuilding(object building)
        {
            return new Dictionary<string, object> {
                { "index", GetMember(building, "index") },
                { "areaIndex", GetMember(building, "areaIndex") },
                { "itemId", GetMember(building, "itemId") },
                { "modelIndex", GetMember(building, "modelIndex") },
                { "localOffsetX", GetMember(building, "localOffset_x") },
                { "localOffsetY", GetMember(building, "localOffset_y") },
                { "localOffsetZ", GetMember(building, "localOffset_z") },
                { "localOffsetX2", GetMember(building, "localOffset_x2") },
                { "localOffsetY2", GetMember(building, "localOffset_y2") },
                { "localOffsetZ2", GetMember(building, "localOffset_z2") },
                { "pitch", GetMember(building, "pitch") },
                { "yaw", GetMember(building, "yaw") },
                { "tilt", GetMember(building, "tilt") },
                { "pitch2", GetMember(building, "pitch2") },
                { "yaw2", GetMember(building, "yaw2") },
                { "tilt2", GetMember(building, "tilt2") },
                { "outputObjectIndex", GetConnectionIndex(building, "outputObj") },
                { "inputObjectIndex", GetConnectionIndex(building, "inputObj") },
                { "temporaryOutputObjectIndex", GetMember(building, "tempOutputObjIdx") },
                { "temporaryInputObjectIndex", GetMember(building, "tempInputObjIdx") },
                { "outputToSlot", GetMember(building, "outputToSlot") },
                { "inputFromSlot", GetMember(building, "inputFromSlot") },
                { "outputFromSlot", GetMember(building, "outputFromSlot") },
                { "inputToSlot", GetMember(building, "inputToSlot") },
                { "outputOffset", GetMember(building, "outputOffset") },
                { "inputOffset", GetMember(building, "inputOffset") }
            };
        }

        private static object ExportReform(object reform)
        {
            return new Dictionary<string, object> {
                { "x", GetMember(reform, "x") },
                { "y", GetMember(reform, "y") },
                { "width", GetMember(reform, "w") },
                { "height", GetMember(reform, "h") },
                { "data", GetMember(reform, "data") },
                { "areaIndex", GetMember(reform, "areaIndex") }
            };
        }

        private static List<object> ExportModelSlotPoseSets(object buildingValues)
        {
            var modelIndices = new SortedSet<int>();
            IEnumerable buildings = buildingValues as IEnumerable;
            if (buildings != null)
            {
                foreach (object building in buildings)
                {
                    object value = GetMember(building, "modelIndex");
                    if (value != null)
                        modelIndices.Add(Convert.ToInt32(value, CultureInfo.InvariantCulture));
                }
            }

            object models = GetStatic(FindType("LDB"), "models", "_models");
            var result = new List<object>();
            foreach (int modelIndex in modelIndices)
            {
                object model = InvokeSelect(models, modelIndex);
                object prefabDesc = GetMember(model, "prefabDesc");
                object slotPoseValues = GetMember(prefabDesc, "slotPoses");
                List<object> slotPoses = ExportSlotPoses(slotPoseValues);
                result.Add(new Dictionary<string, object> {
                    { "modelIndex", modelIndex },
                    { "available", prefabDesc != null },
                    { "slotPoseCount", slotPoses.Count },
                    { "slotPoses", slotPoses }
                });
            }
            return result;
        }

        private static List<object> ExportSlotPoses(object value)
        {
            var result = new List<object>();
            IEnumerable poses = value as IEnumerable;
            if (poses == null) return result;

            int index = 0;
            foreach (object pose in poses)
            {
                object position = GetMember(pose, "position");
                object rotation = GetMember(pose, "rotation");
                result.Add(new Dictionary<string, object> {
                    { "index", index++ },
                    { "position", ExportVector(position) },
                    { "rotation", ExportQuaternion(rotation) }
                });
            }
            return result;
        }

        private static object ExportVector(object vector)
        {
            return new Dictionary<string, object> {
                { "x", GetMember(vector, "x") },
                { "y", GetMember(vector, "y") },
                { "z", GetMember(vector, "z") }
            };
        }

        private static object ExportQuaternion(object quaternion)
        {
            return new Dictionary<string, object> {
                { "x", GetMember(quaternion, "x") },
                { "y", GetMember(quaternion, "y") },
                { "z", GetMember(quaternion, "z") },
                { "w", GetMember(quaternion, "w") }
            };
        }

        private static object InvokeSelect(object instance, int id)
        {
            if (instance == null) return null;
            try
            {
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
            catch
            {
                return null;
            }
        }

        private static object GetConnectionIndex(object building, string memberName)
        {
            object connectedBuilding = GetMember(building, memberName);
            return GetMember(connectedBuilding, "index");
        }

        private static List<object> ExportSequence(
            object value,
            Func<object, object> export)
        {
            var result = new List<object>();
            IEnumerable sequence = value as IEnumerable;
            if (sequence == null) return result;

            foreach (object item in sequence)
                if (item != null) result.Add(export(item));
            return result;
        }

        private static Type FindType(string fullName)
        {
            if (String.IsNullOrEmpty(fullName)) return null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetType(fullName, false);
                    if (type != null) return type;
                }
                catch { }
            }
            return null;
        }

        private static object GetStatic(Type type, params string[] names)
        {
            if (type == null) return null;
            foreach (string name in names)
            {
                object value;
                if (TryGetMember(type, null, name, true, out value))
                    return value;
            }
            return null;
        }

        private static object GetMember(object instance, params string[] names)
        {
            if (instance == null) return null;
            foreach (string name in names)
            {
                object value;
                if (TryGetMember(instance.GetType(), instance, name, false, out value))
                    return value;
            }
            return null;
        }

        private static bool TryGetMember(
            Type type,
            object instance,
            string name,
            bool isStatic,
            out object value)
        {
            value = null;
            try
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                    (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
                    BindingFlags.FlattenHierarchy;

                Type current = type;
                while (current != null)
                {
                    FieldInfo field = current.GetField(name, flags);
                    if (field != null)
                    {
                        value = field.GetValue(instance);
                        return true;
                    }
                    current = current.BaseType;
                }

                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    value = property.GetValue(instance, null);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
