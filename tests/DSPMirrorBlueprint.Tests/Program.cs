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
                AreaMetadataAndTopologyRemainStable,
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
            }
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
