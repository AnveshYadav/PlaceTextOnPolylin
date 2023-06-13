using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace AutoCADCommands
{
    public class PlaceTextOnPolyline
    {
        [CommandMethod("PlaceTextOnPolyline")]
        public void PlaceTextOnPolylineCommand()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            // Prompt user to select a polyline
            PromptEntityResult polylineResult = ed.GetEntity("Select a polyline: ");
            if (polylineResult.Status != PromptStatus.OK || polylineResult.ObjectId.IsNull)
                return;

            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                // Open the selected polyline
                Polyline polyline = tr.GetObject(polylineResult.ObjectId, OpenMode.ForRead) as Polyline;
                if (polyline == null)
                {
                    ed.WriteMessage("Selected entity is not a polyline.");
                    return;
                }

                // Prompt user for the starting number
                PromptIntegerOptions startNumberOptions = new PromptIntegerOptions("\nEnter the starting number: ");
                PromptIntegerResult startNumberResult = ed.GetInteger(startNumberOptions);
                if (startNumberResult.Status != PromptStatus.OK)
                    return;

                int startNumber = startNumberResult.Value;

                // Prompt user for the interval
                PromptDoubleOptions intervalOptions = new PromptDoubleOptions("\nEnter the interval: ");
                PromptDoubleResult intervalResult = ed.GetDouble(intervalOptions);
                if (intervalResult.Status != PromptStatus.OK)
                    return;

                double interval = intervalResult.Value;

                // Prompt user for the distance from the polyline
                PromptDoubleOptions distanceOptions = new PromptDoubleOptions("\nEnter the distance from the polyline: ");
                PromptDoubleResult distanceResult = ed.GetDouble(distanceOptions);
                if (distanceResult.Status != PromptStatus.OK)
                    return;

                double distance = distanceResult.Value;

                // Calculate the length of the polyline
                double polylineLength = polyline.Length;

                // Place text at regular intervals
                double offset = distance;
                int counter = startNumber;
                while (offset <= polylineLength)
                {
                    Point3d point = polyline.GetPointAtDist(offset);
                    double parameter = polyline.GetParameterAtPoint(point);

                    double normalX = 0.0;
                    double normalY = 1.0;

                    if (polyline.GetSegmentType((int)parameter) != SegmentType.Line)
                    {
                        // Calculate the normal direction perpendicular to the polyline at the current parameter
                        Vector3d normal = polyline.GetFirstDerivative(parameter).GetNormal();
                        normalX = normal.X;
                        normalY = normal.Y;
                    }

                    Point3d offsetPoint = new Point3d(point.X + normalX * 10.0, point.Y + normalY * 10.0, point.Z);

                    string text = "STB " + counter.ToString("0.0");

                    using (DBText dbText = new DBText())
                    {
                        dbText.TextString = text;
                        dbText.Position = offsetPoint;

                        // Add the text to the current space
                        BlockTableRecord currentSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        currentSpace.AppendEntity(dbText);
                        tr.AddNewlyCreatedDBObject(dbText, true);
                    }

                    offset += interval;
                    counter++;
                }

                tr.Commit();
            }
        }
    }
}
