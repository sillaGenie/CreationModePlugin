using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModePlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            Level level1 = listLevel
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();
            Level level2 = listLevel
               .Where(x => x.Name.Equals("Уровень 2"))
               .FirstOrDefault();
            List<Wall> walls = CreateWalls(doc, level1, level2, width, depth);
            AddDoor(doc, level1, walls[0]);
            for (int i = 1; i < walls.Count; i++)
            {
                FamilyInstance w = AddWimdow(doc, level1, walls[i]);
                
                
            }
            AddRoof(doc, level2,walls, width, depth); 
            return Result.Succeeded;
        }

        private void AddRoof(Document doc, Level level2, List<Wall> walls, double width, double depth)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                 .OfClass(typeof(RoofType))
                 .OfType<RoofType>()
                 .Where(x => x.Name.Equals("Типовой - 400мм"))
                 .Where(x => x.FamilyName.Equals("Базовая крыша"))
                 .FirstOrDefault();
            double wallWidth = walls[0].Width;
            double dx = width / 2;
            double dy = depth / 2;
            double dz = level2.Elevation;
            double dz1 = dz*2;



            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(-dx - wallWidth / 2, -dy - wallWidth / 2, dz), new XYZ(-dx - wallWidth / 2, 0, dz1)));
            curveArray.Append(Line.CreateBound(new XYZ(-dx - wallWidth / 2, 0, dz1), new XYZ(-dx - wallWidth / 2, dy + wallWidth / 2, dz)));



            Application application = doc.Application;
            CurveArray footprint = doc.Application.Create.NewCurveArray();
            
                
               
            

            Transaction transaction4 = new Transaction(doc, "Построение крыши");
            
                transaction4.Start();
            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20 ), new XYZ(0, dy, 0), doc.ActiveView);
                doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, -dx - wallWidth / 2, dx + wallWidth / 2);
            transaction4.Commit();
            


        }

        public static void AddDoor(Document doc, Level level1, Wall wall)
        {
            FamilySymbol doorType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("Одиночные-Щитовые"))
                .FirstOrDefault();
           
            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
           
            Transaction transaction2 = new Transaction(doc, "Построение двери");
            transaction2.Start();
            if (!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
            transaction2.Commit();
            
        }
        public FamilyInstance AddWimdow(Document doc, Level level1, Wall wall)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            Transaction transaction3 = new Transaction(doc, "Построение окна");
            transaction3.Start();
            if (!windowType.IsActive)
                windowType.Activate();
            FamilyInstance w =  doc.Create.NewFamilyInstance(point, windowType, wall, level1, StructuralType.NonStructural);
            w.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(3);
            transaction3.Commit();
            return w;
        }
        public List<Wall> CreateWalls(Document doc, Level level1, Level level2, double width, double depth)
        {

            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));
            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc, "Построение стен");
            transaction.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            transaction.Commit();
            return walls;
        }
        
    }
}
