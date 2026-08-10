using System;
using System.Collections.Generic;

namespace DSPMirrorBlueprint.Tests
{
    internal static class Program
    {
        private const float Tolerance = 0.00001f;

        private static int Main()
        {
            var tests = new List<Action> {
                HorizontalMirrorReflectsGeometry,
                VerticalMirrorReflectsGeometry,
                DoubleMirrorRestoresOriginal,
                OddSizedCenterlinesStayFixed,
                MixedAxisMirrorsCommute,
                ConnectionSlotsFollowMirroredPrefabPoses,
                CoincidentSlotPositionsUseOrientation,
                MissingOrUnmatchedSlotPosesRemainStable,
                AreaMetadataAndTopologyRemainStable,
                MultiAreaFixtureUsesAggregateBounds,
                ReformRectanglesReflectAtBounds,
                InvalidBoundsAreRejected
            };

            try
            {
                foreach (Action test in tests) test();
                Console.WriteLine(
                    "DSPMirrorBlueprint deterministic tests passed: " +
                    tests.Count + ".");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("DSPMirrorBlueprint deterministic test failed: " + ex);
                return 1;
            }
        }

        private static void HorizontalMirrorReflectsGeometry()
        {
            BlueprintTransformModel model = CreateModel();
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Horizontal);

            BlueprintTransformBuilding building = model.Buildings[0];
            Equal(1.25f, building.Position.X, "horizontal position x");
            Equal(3.5f, building.Position.Y, "horizontal position y");
            Equal(2f, building.Position.Z, "horizontal position z");
            Equal(3.75f, building.Position2.X, "horizontal endpoint x");
            Equal(0.75f, building.Position2.Y, "horizontal endpoint y");
            Equal(3f, building.Position2.Z, "horizontal endpoint z");
            Equal(0.2f, building.Orientation.Forward.X, "horizontal forward x");
            Equal(-0.8f, building.Orientation.Forward.Y, "horizontal forward y");
            Equal(0.5f, building.Orientation.Forward.Z, "horizontal forward z");
            Equal(-0.1f, building.Orientation.Up.X, "horizontal up x");
            Equal(-0.3f, building.Orientation.Up.Y, "horizontal up y");
            Equal(0.9f, building.Orientation.Up.Z, "horizontal up z");
            Equal(-0.4f, building.Orientation2.Forward.X, "horizontal endpoint forward x");
            Equal(-0.1f, building.Orientation2.Forward.Y, "horizontal endpoint forward y");
            Equal(0.7f, building.Orientation2.Forward.Z, "horizontal endpoint forward z");
            Equal(0.6f, building.Orientation2.Up.X, "horizontal endpoint up x");
            Equal(0.2f, building.Orientation2.Up.Y, "horizontal endpoint up y");
            Equal(0.5f, building.Orientation2.Up.Z, "horizontal endpoint up z");
            Equal(1, model.Reforms[0].X, "horizontal reform x");
            Equal(3, model.Reforms[0].Y, "horizontal reform y");
            Equal(2, model.CursorOffsetX, "horizontal cursor x");
            Equal(1, model.CursorOffsetY, "horizontal cursor y");
        }

        private static void VerticalMirrorReflectsGeometry()
        {
            BlueprintTransformModel model = CreateModel();
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);

            BlueprintTransformBuilding building = model.Buildings[0];
            Equal(3.75f, building.Position.X, "vertical position x");
            Equal(1.5f, building.Position.Y, "vertical position y");
            Equal(2f, building.Position.Z, "vertical position z");
            Equal(1.25f, building.Position2.X, "vertical endpoint x");
            Equal(4.25f, building.Position2.Y, "vertical endpoint y");
            Equal(3f, building.Position2.Z, "vertical endpoint z");
            Equal(-0.2f, building.Orientation.Forward.X, "vertical forward x");
            Equal(0.8f, building.Orientation.Forward.Y, "vertical forward y");
            Equal(0.5f, building.Orientation.Forward.Z, "vertical forward z");
            Equal(0.1f, building.Orientation.Up.X, "vertical up x");
            Equal(0.3f, building.Orientation.Up.Y, "vertical up y");
            Equal(0.9f, building.Orientation.Up.Z, "vertical up z");
            Equal(0.4f, building.Orientation2.Forward.X, "vertical endpoint forward x");
            Equal(0.1f, building.Orientation2.Forward.Y, "vertical endpoint forward y");
            Equal(0.7f, building.Orientation2.Forward.Z, "vertical endpoint forward z");
            Equal(-0.6f, building.Orientation2.Up.X, "vertical endpoint up x");
            Equal(-0.2f, building.Orientation2.Up.Y, "vertical endpoint up y");
            Equal(0.5f, building.Orientation2.Up.Z, "vertical endpoint up z");
            Equal(2, model.Reforms[0].X, "vertical reform x");
            Equal(1, model.Reforms[0].Y, "vertical reform y");
            Equal(3, model.CursorOffsetX, "vertical cursor x");
            Equal(4, model.CursorOffsetY, "vertical cursor y");
        }

        private static void DoubleMirrorRestoresOriginal()
        {
            foreach (BlueprintMirrorAxis axis in new[] {
                BlueprintMirrorAxis.Horizontal,
                BlueprintMirrorAxis.Vertical
            })
            {
                BlueprintTransformModel model = CreateModel();
                BlueprintMirrorTransform.Apply(model, axis);
                BlueprintMirrorTransform.Apply(model, axis);

                BlueprintTransformBuilding building = model.Buildings[0];
                Equal(1.25f, building.Position.X, axis + " restored x");
                Equal(1.5f, building.Position.Y, axis + " restored y");
                Equal(3.75f, building.Position2.X, axis + " restored endpoint x");
                Equal(4.25f, building.Position2.Y, axis + " restored endpoint y");
                Equal(0.2f, building.Orientation.Forward.X, axis + " restored forward x");
                Equal(0.8f, building.Orientation.Forward.Y, axis + " restored forward y");
                Equal(-0.1f, building.Orientation.Up.X, axis + " restored up x");
                Equal(0.3f, building.Orientation.Up.Y, axis + " restored up y");
                Equal(1, model.Reforms[0].X, axis + " restored reform x");
                Equal(1, model.Reforms[0].Y, axis + " restored reform y");
                Equal(2, model.CursorOffsetX, axis + " restored cursor x");
                Equal(4, model.CursorOffsetY, axis + " restored cursor y");
                Equal(10, building.InputFromSlot, axis + " restored input slot");
                Equal(9, building.OutputToSlot, axis + " restored output slot");
            }
        }

        private static void OddSizedCenterlinesStayFixed()
        {
            var model = new BlueprintTransformModel {
                Width = 5,
                Height = 7,
                CursorOffsetX = 2,
                CursorOffsetY = 3
            };
            model.Buildings.Add(new BlueprintTransformBuilding {
                Position = new BlueprintVector3(2f, 3f, 1f),
                Position2 = new BlueprintVector3(0f, 6f, 2f)
            });
            model.Reforms.Add(new BlueprintTransformReform {
                X = 2,
                Y = 3,
                Width = 1,
                Height = 1
            });

            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Horizontal);
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);

            Equal(2f, model.Buildings[0].Position.X, "odd center building x");
            Equal(3f, model.Buildings[0].Position.Y, "odd center building y");
            Equal(4f, model.Buildings[0].Position2.X, "odd edge endpoint x");
            Equal(0f, model.Buildings[0].Position2.Y, "odd edge endpoint y");
            Equal(2, model.Reforms[0].X, "odd center reform x");
            Equal(3, model.Reforms[0].Y, "odd center reform y");
            Equal(2, model.CursorOffsetX, "odd center cursor x");
            Equal(3, model.CursorOffsetY, "odd center cursor y");
        }

        private static void MixedAxisMirrorsCommute()
        {
            BlueprintTransformModel horizontalThenVertical = CreateModel();
            BlueprintMirrorTransform.Apply(
                horizontalThenVertical,
                BlueprintMirrorAxis.Horizontal);
            BlueprintMirrorTransform.Apply(
                horizontalThenVertical,
                BlueprintMirrorAxis.Vertical);

            BlueprintTransformModel verticalThenHorizontal = CreateModel();
            BlueprintMirrorTransform.Apply(
                verticalThenHorizontal,
                BlueprintMirrorAxis.Vertical);
            BlueprintMirrorTransform.Apply(
                verticalThenHorizontal,
                BlueprintMirrorAxis.Horizontal);

            BlueprintTransformBuilding left = horizontalThenVertical.Buildings[0];
            BlueprintTransformBuilding right = verticalThenHorizontal.Buildings[0];
            Equal(left.Position.X, right.Position.X, "mixed-axis position x");
            Equal(left.Position.Y, right.Position.Y, "mixed-axis position y");
            Equal(left.Position2.X, right.Position2.X, "mixed-axis endpoint x");
            Equal(left.Position2.Y, right.Position2.Y, "mixed-axis endpoint y");
            Equal(
                left.Orientation.Forward.X,
                right.Orientation.Forward.X,
                "mixed-axis forward x");
            Equal(
                left.Orientation.Forward.Y,
                right.Orientation.Forward.Y,
                "mixed-axis forward y");
            Equal(left.InputFromSlot, right.InputFromSlot, "mixed-axis input slot");
            Equal(left.OutputToSlot, right.OutputToSlot, "mixed-axis output slot");
            Equal(
                horizontalThenVertical.Reforms[0].X,
                verticalThenHorizontal.Reforms[0].X,
                "mixed-axis reform x");
            Equal(
                horizontalThenVertical.Reforms[0].Y,
                verticalThenHorizontal.Reforms[0].Y,
                "mixed-axis reform y");
            Equal(
                horizontalThenVertical.CursorOffsetX,
                verticalThenHorizontal.CursorOffsetX,
                "mixed-axis cursor x");
            Equal(
                horizontalThenVertical.CursorOffsetY,
                verticalThenHorizontal.CursorOffsetY,
                "mixed-axis cursor y");
        }

        private static void ConnectionSlotsFollowMirroredPrefabPoses()
        {
            foreach (BlueprintMirrorAxis axis in new[] {
                BlueprintMirrorAxis.Horizontal,
                BlueprintMirrorAxis.Vertical
            })
            {
                BlueprintTransformModel model = CreateModel();
                BlueprintMirrorTransform.Apply(model, axis);
                Equal(4, model.Buildings[0].InputFromSlot, axis + " input slot");
                Equal(5, model.Buildings[0].OutputToSlot, axis + " output slot");
                Equal(0, model.Buildings[0].OutputFromSlot, axis + " local output slot");
                Equal(1, model.Buildings[0].InputToSlot, axis + " local input slot");

                model = CreateModel();
                model.Buildings[0].OutputToSlot = -1;
                BlueprintMirrorTransform.Apply(model, axis);
                Equal(-1, model.Buildings[0].OutputToSlot, axis + " belt sentinel slot");
            }
        }

        private static void CoincidentSlotPositionsUseOrientation()
        {
            BlueprintTransformModel model = CreateModel();
            model.Buildings[0].OutputToSlot = 3;
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);
            Equal(11, model.Buildings[0].OutputToSlot, "orientation-disambiguated slot");
        }

        private static void MissingOrUnmatchedSlotPosesRemainStable()
        {
            BlueprintTransformModel model = CreateModel();
            model.Buildings[1].ModelIndex = 404;
            model.Buildings[2].ModelIndex = 404;
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);
            Equal(10, model.Buildings[0].InputFromSlot, "missing model input slot");
            Equal(9, model.Buildings[0].OutputToSlot, "missing model output slot");

            model = CreateModel();
            model.Buildings[0].InputFromSlot = 99;
            model.Buildings[0].OutputToSlot = 98;
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Horizontal);
            Equal(99, model.Buildings[0].InputFromSlot, "unknown input slot");
            Equal(98, model.Buildings[0].OutputToSlot, "unknown output slot");

            model = CreateModel();
            model.Buildings[2].ModelIndex = 66;
            model.Buildings[0].OutputToSlot = 3;
            model.SlotPosesByModelIndex.Add(
                66,
                new List<BlueprintTransformSlotPose> {
                    CreateSlot(3, 1f, 0f, 1f)
                });
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);
            Equal(3, model.Buildings[0].OutputToSlot, "unmatched reflected slot");
        }

        private static void AreaMetadataAndTopologyRemainStable()
        {
            BlueprintTransformModel model = CreateModel();
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Horizontal);
            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);

            BlueprintTransformArea child = model.Areas[1];
            Equal(1, child.Index, "area index");
            Equal(0, child.ParentIndex, "area parent");
            Equal(120, child.AreaSegments, "area segments");
            Equal(5, child.AnchorLocalOffsetY, "area anchor y");
            Equal(21, child.Width, "area width");
            Equal(10, child.Height, "area height");

            BlueprintTransformBuilding building = model.Buildings[0];
            Equal(7, building.InputObjectIndex.Value, "input connection");
            Equal(9, building.OutputObjectIndex.Value, "output connection");
            Equal(0, building.AreaIndex, "building area index");
            Equal(0, model.Reforms[0].AreaIndex, "reform area index");
            Equal(32, model.Reforms[0].Data, "reform data");
        }

        private static void MultiAreaFixtureUsesAggregateBounds()
        {
            var model = new BlueprintTransformModel {
                Width = 27,
                Height = 15,
                CursorOffsetX = 26,
                CursorOffsetY = 14
            };
            model.Areas.Add(new BlueprintTransformArea {
                Index = 0,
                ParentIndex = -1,
                AreaSegments = 160,
                Width = 27,
                Height = 5
            });
            model.Areas.Add(new BlueprintTransformArea {
                Index = 1,
                ParentIndex = 0,
                AreaSegments = 120,
                AnchorLocalOffsetY = 5,
                Width = 21,
                Height = 10
            });
            model.Buildings.Add(new BlueprintTransformBuilding {
                Index = 0,
                AreaIndex = 0,
                Position = new BlueprintVector3(26f, 14f, 0f),
                Position2 = new BlueprintVector3(0f, 0f, 0f)
            });
            model.Reforms.Add(new BlueprintTransformReform {
                AreaIndex = 0,
                X = 19,
                Y = 3,
                Width = 3,
                Height = 2,
                Data = 32
            });
            model.Reforms.Add(new BlueprintTransformReform {
                AreaIndex = 0,
                X = 25,
                Y = 3,
                Width = 1,
                Height = 2,
                Data = 32
            });

            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Vertical);
            Equal(0f, model.Buildings[0].Position.X, "multi-area position x");
            Equal(26f, model.Buildings[0].Position2.X, "multi-area endpoint x");
            Equal(5, model.Reforms[0].X, "multi-area wide reform x");
            Equal(1, model.Reforms[1].X, "multi-area narrow reform x");

            BlueprintMirrorTransform.Apply(model, BlueprintMirrorAxis.Horizontal);
            Equal(0f, model.Buildings[0].Position.Y, "multi-area position y");
            Equal(14f, model.Buildings[0].Position2.Y, "multi-area endpoint y");
            Equal(10, model.Reforms[0].Y, "multi-area wide reform y");
            Equal(10, model.Reforms[1].Y, "multi-area narrow reform y");
            Equal(0, model.CursorOffsetX, "multi-area cursor x");
            Equal(0, model.CursorOffsetY, "multi-area cursor y");
            Equal(120, model.Areas[1].AreaSegments, "multi-area segments");
            Equal(5, model.Areas[1].AnchorLocalOffsetY, "multi-area anchor");
        }

        private static void ReformRectanglesReflectAtBounds()
        {
            var vertical = new BlueprintTransformModel { Width = 8, Height = 6 };
            vertical.Reforms.Add(new BlueprintTransformReform {
                X = 0,
                Y = 1,
                Width = 2,
                Height = 3
            });
            vertical.Reforms.Add(new BlueprintTransformReform {
                X = 6,
                Y = 1,
                Width = 2,
                Height = 3
            });
            BlueprintMirrorTransform.Apply(vertical, BlueprintMirrorAxis.Vertical);
            Equal(6, vertical.Reforms[0].X, "left-bound reform");
            Equal(0, vertical.Reforms[1].X, "right-bound reform");

            var horizontal = new BlueprintTransformModel { Width = 8, Height = 6 };
            horizontal.Reforms.Add(new BlueprintTransformReform {
                X = 1,
                Y = 0,
                Width = 3,
                Height = 2
            });
            horizontal.Reforms.Add(new BlueprintTransformReform {
                X = 1,
                Y = 4,
                Width = 3,
                Height = 2
            });
            BlueprintMirrorTransform.Apply(horizontal, BlueprintMirrorAxis.Horizontal);
            Equal(4, horizontal.Reforms[0].Y, "bottom-bound reform");
            Equal(0, horizontal.Reforms[1].Y, "top-bound reform");
        }

        private static void InvalidBoundsAreRejected()
        {
            bool threw = false;
            try
            {
                BlueprintMirrorTransform.Apply(
                    new BlueprintTransformModel(),
                    BlueprintMirrorAxis.Horizontal);
            }
            catch (ArgumentOutOfRangeException)
            {
                threw = true;
            }

            if (!threw) throw new InvalidOperationException("invalid bounds were accepted");
        }

        private static BlueprintTransformModel CreateModel()
        {
            var model = new BlueprintTransformModel {
                Width = 6,
                Height = 6,
                CursorOffsetX = 2,
                CursorOffsetY = 4
            };
            model.Areas.Add(new BlueprintTransformArea {
                Index = 0,
                ParentIndex = -1,
                AreaSegments = 160,
                Width = 6,
                Height = 2
            });
            model.Areas.Add(new BlueprintTransformArea {
                Index = 1,
                ParentIndex = 0,
                AreaSegments = 120,
                AnchorLocalOffsetY = 5,
                Width = 21,
                Height = 10
            });
            model.Buildings.Add(new BlueprintTransformBuilding {
                Index = 4,
                AreaIndex = 0,
                InputObjectIndex = 7,
                OutputObjectIndex = 9,
                InputFromSlot = 10,
                OutputToSlot = 9,
                OutputFromSlot = 0,
                InputToSlot = 1,
                Position = new BlueprintVector3(1.25f, 1.5f, 2f),
                Position2 = new BlueprintVector3(3.75f, 4.25f, 3f),
                Orientation = new BlueprintOrientation {
                    Forward = new BlueprintVector3(0.2f, 0.8f, 0.5f),
                    Up = new BlueprintVector3(-0.1f, 0.3f, 0.9f)
                },
                Orientation2 = new BlueprintOrientation {
                    Forward = new BlueprintVector3(-0.4f, 0.1f, 0.7f),
                    Up = new BlueprintVector3(0.6f, -0.2f, 0.5f)
                }
            });
            model.Buildings.Add(new BlueprintTransformBuilding {
                Index = 7,
                ModelIndex = 65,
                Orientation = new BlueprintOrientation(),
                Orientation2 = new BlueprintOrientation()
            });
            model.Buildings.Add(new BlueprintTransformBuilding {
                Index = 9,
                ModelIndex = 65,
                Orientation = new BlueprintOrientation(),
                Orientation2 = new BlueprintOrientation()
            });
            AddSlotPair(model, 0, 2, -1f, 1f, 1f, 0f);
            AddSlotPair(model, 3, 11, 1f, -1f, 1f, 1f);
            AddSlotPair(model, 4, 10, 1.1f, -1.1f, 0f, 1f);
            AddSlotPair(model, 5, 9, 1.1f, -1.1f, -1f, 1f);
            model.Reforms.Add(new BlueprintTransformReform {
                AreaIndex = 0,
                X = 1,
                Y = 1,
                Width = 3,
                Height = 2,
                Data = 32
            });
            return model;
        }

        private static void AddSlotPair(
            BlueprintTransformModel model,
            int leftIndex,
            int rightIndex,
            float leftX,
            float rightX,
            float y,
            float forwardX)
        {
            List<BlueprintTransformSlotPose> poses;
            if (!model.SlotPosesByModelIndex.TryGetValue(65, out poses))
            {
                poses = new List<BlueprintTransformSlotPose>();
                model.SlotPosesByModelIndex.Add(65, poses);
            }

            poses.Add(CreateSlot(leftIndex, leftX, y, forwardX));
            poses.Add(CreateSlot(rightIndex, rightX, y, -forwardX));
        }

        private static BlueprintTransformSlotPose CreateSlot(
            int index,
            float x,
            float y,
            float forwardX)
        {
            return new BlueprintTransformSlotPose {
                Index = index,
                Position = new BlueprintVector3(x, y, 0f),
                Orientation = new BlueprintOrientation {
                    Forward = new BlueprintVector3(forwardX, 1f - Math.Abs(forwardX), 0f),
                    Up = new BlueprintVector3(0f, 0f, 1f)
                }
            };
        }

        private static void Equal(float expected, float actual, string name)
        {
            if (Math.Abs(expected - actual) > Tolerance)
                throw new InvalidOperationException(
                    name + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal(int expected, int actual, string name)
        {
            if (expected != actual)
                throw new InvalidOperationException(
                    name + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
