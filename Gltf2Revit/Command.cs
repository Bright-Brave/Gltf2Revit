#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Application = Autodesk.Revit.ApplicationServices.Application;

#endregion

namespace Gltf2Revit
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        private const double m_to_foot = 1000.0 / 304.8;
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(MyResolveEventHandler);

            var gltfModel = SharpGLTF.Schema2.ModelRoot.Load(
                @"E:\ModelPipe_Files\Ifc_files\电塔.glb");

            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
            builder.OpenConnectedFaceSet(false);
            List<XYZ> loopVertices = new List<XYZ>(3);

            for (int i = 0; i < gltfModel.LogicalMeshes.Count; i++)
            {
                var mesh = gltfModel.LogicalMeshes[i];
                for (int j = 0; j < mesh.Primitives.Count; j++)
                {
                    var primitive = mesh.Primitives[j];
                    var vertices = primitive.GetVertices("POSITION").AsVector3Array();

                    for (int k = 0; k < vertices.Count / 3; k+=3)
                    {
                        loopVertices.Clear();
                        loopVertices.Add(new XYZ(
                            vertices[k].X * m_to_foot, 
                            vertices[k].Z * m_to_foot, 
                            vertices[k].Y * m_to_foot));
                        loopVertices.Add(new XYZ(
                            vertices[k + 1].X * m_to_foot, 
                            vertices[k + 1].Z * m_to_foot, 
                            vertices[k + 1].Y * m_to_foot));
                        loopVertices.Add(new XYZ(
                            vertices[k + 2].X * m_to_foot, 
                            vertices[k + 2].Z * m_to_foot, 
                            vertices[k + 2].Y * m_to_foot));

                        var tesseFace = new TessellatedFace(loopVertices, ElementId.InvalidElementId);
                        if (builder.DoesFaceHaveEnoughLoopsAndVertices(tesseFace))
                        {
                            builder.AddFace(tesseFace);
                        }
                    }

                    //MessageBox.Show($"Key: {f[k]}", "提示");
                }
            }

            builder.CloseConnectedFaceSet();
            builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
            builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
            builder.Build();

            TessellatedShapeBuilderResult result = builder.GetBuildResult();
            // Modify document within a transaction

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Transaction Name");

                DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                ds.ApplicationId = Assembly.GetExecutingAssembly().GetType().GUID.ToString();
                ds.ApplicationDataId = Guid.NewGuid().ToString();

                ds.SetShape(result.GetGeometricalObjects());

                ds.Name = "MyShape";
                tx.Commit();
            }

            return Result.Succeeded;
        }

        private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            string assemblyLoc = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var text = $"{Path.Combine(assemblyLoc, assemblyName.Name)}.dll";
            Assembly result;
            if (File.Exists(text))
            {
                result = Assembly.LoadFrom(text);
            }
            else
            {
                //TaskDialog.Show("ERROR", $"未找到以下程序集:{text}");
                result = null;
            }
            return result;
        }
    }
}
