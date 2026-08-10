using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace DSPMirrorBlueprint
{
    internal static class GameInputBridge
    {
        private const int KeyCodeK = (int)KeyCode.K;
        private const byte NoModifier = 0;
        private const byte ShiftModifier = 1;

        private static readonly BindingFlags StaticFlags = BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly BindingFlags InstanceFlags = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;

        private static ManualLogSource logger;
        private static Func<bool> diagnosticsEnabled;
        private static Type vfInputType;
        private static Type combineKeyType;
        private static FieldInfo overrideKeysField;
        private static FieldInfo axisCombineKeyField;
        private static FieldInfo keyCodeField;
        private static FieldInfo modifierField;
        private static FieldInfo noneKeyField;
        private static FieldInfo downField;
        private static FieldInfo pressField;
        private static FieldInfo lastPressField;
        private static FieldInfo upField;
        private static FieldInfo eventFrameField;
        private static ConstructorInfo combineKeyConstructor;
        private static object onceClickAction;
        private static int horizontalSlot = -1;
        private static int verticalSlot = -1;
        private static int tracedHorizontalFrame = -1;
        private static int tracedVerticalFrame = -1;

        public static bool Install(
            Harmony harmony,
            ManualLogSource log,
            Func<bool> isDiagnosticsEnabled,
            out string error)
        {
            logger = log;
            diagnosticsEnabled = isDiagnosticsEnabled;
            error = null;

            try
            {
                vfInputType = AccessTools.TypeByName("VFInput");
                combineKeyType = AccessTools.TypeByName("CombineKey");
                Type actionType = AccessTools.TypeByName("ECombineKeyAction");
                if (vfInputType == null || combineKeyType == null || actionType == null)
                {
                    error = "the game's input binding types were not found.";
                    return false;
                }

                overrideKeysField = vfInputType.GetField("override_keys", StaticFlags);
                axisCombineKeyField = vfInputType.GetField("axis_combine_key", StaticFlags);
                keyCodeField = combineKeyType.GetField("keyCode", InstanceFlags);
                modifierField = combineKeyType.GetField("modifier", InstanceFlags);
                noneKeyField = combineKeyType.GetField("noneKey", InstanceFlags);
                combineKeyConstructor = combineKeyType.GetConstructor(
                    InstanceFlags,
                    null,
                    new[] { typeof(int), typeof(byte), actionType, typeof(bool) },
                    null);
                onceClickAction = Enum.ToObject(actionType, 0);

                Type inputAxisType = AccessTools.Inner(vfInputType, "InputAxis");
                downField = inputAxisType == null
                    ? null
                    : inputAxisType.GetField("down", InstanceFlags);
                pressField = inputAxisType == null
                    ? null
                    : inputAxisType.GetField("press", InstanceFlags);
                lastPressField = inputAxisType == null
                    ? null
                    : inputAxisType.GetField("lastPress", InstanceFlags);
                upField = inputAxisType == null
                    ? null
                    : inputAxisType.GetField("up", InstanceFlags);
                eventFrameField = inputAxisType == null
                    ? null
                    : inputAxisType.GetField("eventFrame", InstanceFlags);

                if (overrideKeysField == null || axisCombineKeyField == null ||
                    keyCodeField == null || modifierField == null || noneKeyField == null ||
                    combineKeyConstructor == null || downField == null ||
                    eventFrameField == null)
                {
                    error = "the game's captured override-key state was incomplete.";
                    return false;
                }

                PatchPostfix(harmony, vfInputType, "Init", "VFInputInitPostfix");
                PatchPostfix(harmony, vfInputType, "OnFixedUpdate", "VFInputFixedUpdatePostfix");

                Type gameOptionType = AccessTools.TypeByName("GameOption");
                MethodInfo apply = gameOptionType == null
                    ? null
                    : AccessTools.Method(gameOptionType, "Apply", Type.EmptyTypes);
                if (apply != null)
                {
                    harmony.Patch(
                        apply,
                        postfix: new HarmonyMethod(
                            AccessTools.Method(typeof(GameInputBridge), "GameOptionApplyPostfix")));
                }

                EnsureBindings();
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
            try
            {
                Array bindings = overrideKeysField == null
                    ? null
                    : overrideKeysField.GetValue(null) as Array;
                ClearOwnedBinding(bindings, horizontalSlot, NoModifier);
                ClearOwnedBinding(bindings, verticalSlot, ShiftModifier);
            }
            catch (Exception)
            {
            }

            horizontalSlot = -1;
            verticalSlot = -1;
            tracedHorizontalFrame = -1;
            tracedVerticalFrame = -1;
        }

        public static bool TryGetMirrorAxis(
            int frame,
            ref int handledFrame,
            out BlueprintMirrorAxis axis)
        {
            bool horizontalDown;
            bool verticalDown;
            int horizontalEventFrame;
            int verticalEventFrame;
            if (!TryReadCapturedState(
                out horizontalDown,
                out verticalDown,
                out horizontalEventFrame,
                out verticalEventFrame))
            {
                axis = BlueprintMirrorAxis.Horizontal;
                return false;
            }

            bool selected = MirrorInputDecision.TrySelect(
                horizontalDown,
                verticalDown,
                frame,
                ref handledFrame,
                out axis);
            if (selected && IsDiagnosticsEnabled())
            {
                logger.LogInfo(
                    "Mirror input observed by blueprint paste: frame=" + frame +
                    ", K.down=" + horizontalDown +
                    ", K.eventFrame=" + horizontalEventFrame +
                    ", Shift+K.down=" + verticalDown +
                    ", Shift+K.eventFrame=" + verticalEventFrame + ".");
            }
            return selected;
        }

        public static void LogMirrorResult(
            BlueprintMirrorAxis axis,
            bool applied,
            string error)
        {
            if (!IsDiagnosticsEnabled()) return;
            logger.LogInfo(
                "Mirror input result: axis=" + axis +
                ", applied=" + applied +
                (String.IsNullOrEmpty(error) ? "." : ", error=" + error + "."));
        }

        private static void PatchPostfix(
            Harmony harmony,
            Type type,
            string targetName,
            string postfixName)
        {
            MethodInfo target = AccessTools.Method(type, targetName, Type.EmptyTypes);
            MethodInfo postfix = AccessTools.Method(typeof(GameInputBridge), postfixName);
            if (target == null || postfix == null)
                throw new MissingMethodException(type.FullName, targetName);
            harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        }

        private static void VFInputInitPostfix()
        {
            EnsureBindings();
        }

        private static void GameOptionApplyPostfix()
        {
            EnsureBindings();
        }

        private static void VFInputFixedUpdatePostfix()
        {
            if (!IsDiagnosticsEnabled()) return;

            bool horizontalDown;
            bool verticalDown;
            int horizontalEventFrame;
            int verticalEventFrame;
            if (!TryReadCapturedState(
                out horizontalDown,
                out verticalDown,
                out horizontalEventFrame,
                out verticalEventFrame))
            {
                return;
            }

            if (horizontalDown && tracedHorizontalFrame != horizontalEventFrame)
            {
                tracedHorizontalFrame = horizontalEventFrame;
                logger.LogInfo(
                    "Mirror input captured by VFInput: binding=K, slot=" +
                    horizontalSlot + ", eventFrame=" + horizontalEventFrame + ".");
            }
            if (verticalDown && tracedVerticalFrame != verticalEventFrame)
            {
                tracedVerticalFrame = verticalEventFrame;
                logger.LogInfo(
                    "Mirror input captured by VFInput: binding=Shift+K, slot=" +
                    verticalSlot + ", eventFrame=" + verticalEventFrame + ".");
            }
        }

        private static void EnsureBindings()
        {
            Array bindings = overrideKeysField == null
                ? null
                : overrideKeysField.GetValue(null) as Array;
            if (bindings == null) return;

            horizontalSlot = EnsureBinding(bindings, horizontalSlot, NoModifier);
            verticalSlot = EnsureBinding(bindings, verticalSlot, ShiftModifier);
            if ((horizontalSlot < 0 || verticalSlot < 0) && logger != null)
                logger.LogError("DSP input capture has no free override-key slots for mirroring.");
        }

        private static int EnsureBinding(Array bindings, int slot, byte modifier)
        {
            if (slot >= 0 && slot < bindings.Length)
            {
                object current = bindings.GetValue(slot);
                if (IsOwnedBinding(current, modifier)) return slot;
                if (IsNullBinding(current))
                {
                    SetBinding(bindings, slot, modifier);
                    return slot;
                }
            }

            for (int candidate = bindings.Length - 1; candidate >= 0; candidate--)
            {
                if (candidate == horizontalSlot || candidate == verticalSlot) continue;
                if (!IsNullBinding(bindings.GetValue(candidate))) continue;
                SetBinding(bindings, candidate, modifier);
                return candidate;
            }
            return -1;
        }

        private static void SetBinding(Array bindings, int slot, byte modifier)
        {
            object binding = combineKeyConstructor.Invoke(
                new[] { (object)KeyCodeK, modifier, onceClickAction, false });
            bindings.SetValue(binding, slot);
            ClearAxisSlot(slot);
        }

        private static void ClearOwnedBinding(Array bindings, int slot, byte modifier)
        {
            if (bindings == null || slot < 0 || slot >= bindings.Length) return;
            if (!IsOwnedBinding(bindings.GetValue(slot), modifier)) return;
            bindings.SetValue(Activator.CreateInstance(combineKeyType), slot);
            ClearAxisSlot(slot);
        }

        private static bool IsOwnedBinding(object binding, byte modifier)
        {
            return binding != null &&
                Convert.ToInt32(keyCodeField.GetValue(binding)) == KeyCodeK &&
                Convert.ToByte(modifierField.GetValue(binding)) == modifier &&
                !Convert.ToBoolean(noneKeyField.GetValue(binding));
        }

        private static bool IsNullBinding(object binding)
        {
            return binding == null ||
                (Convert.ToInt32(keyCodeField.GetValue(binding)) == 0 &&
                 Convert.ToByte(modifierField.GetValue(binding)) == 0 &&
                 !Convert.ToBoolean(noneKeyField.GetValue(binding)));
        }

        private static bool TryReadCapturedState(
            out bool horizontalDown,
            out bool verticalDown,
            out int horizontalEventFrame,
            out int verticalEventFrame)
        {
            horizontalDown = false;
            verticalDown = false;
            horizontalEventFrame = -1;
            verticalEventFrame = -1;
            try
            {
                if (horizontalSlot < 0 || verticalSlot < 0)
                    EnsureBindings();
                if (horizontalSlot < 0 || verticalSlot < 0) return false;

                object axis = axisCombineKeyField.GetValue(null);
                if (axis == null) return false;
                bool[] down = downField.GetValue(axis) as bool[];
                int[] eventFrames = eventFrameField.GetValue(axis) as int[];
                if (down == null || eventFrames == null ||
                    horizontalSlot >= down.Length || verticalSlot >= down.Length)
                {
                    return false;
                }

                horizontalDown = down[horizontalSlot];
                verticalDown = down[verticalSlot];
                horizontalEventFrame = eventFrames[horizontalSlot];
                verticalEventFrame = eventFrames[verticalSlot];
                return true;
            }
            catch (Exception ex)
            {
                if (logger != null)
                    logger.LogWarning("DSP input capture could not be read: " + ex.Message);
                return false;
            }
        }

        private static void ClearAxisSlot(int slot)
        {
            if (slot < 0 || axisCombineKeyField == null) return;
            object axis = axisCombineKeyField.GetValue(null);
            if (axis == null) return;
            ClearBooleanArray(pressField, axis, slot);
            ClearBooleanArray(lastPressField, axis, slot);
            ClearBooleanArray(downField, axis, slot);
            ClearBooleanArray(upField, axis, slot);
            int[] eventFrames = eventFrameField.GetValue(axis) as int[];
            if (eventFrames != null && slot < eventFrames.Length) eventFrames[slot] = 0;
        }

        private static void ClearBooleanArray(FieldInfo field, object axis, int slot)
        {
            bool[] values = field == null ? null : field.GetValue(axis) as bool[];
            if (values != null && slot < values.Length) values[slot] = false;
        }

        private static bool IsDiagnosticsEnabled()
        {
            return logger != null && diagnosticsEnabled != null && diagnosticsEnabled();
        }
    }
}
