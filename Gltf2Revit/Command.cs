#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Application = Autodesk.Revit.ApplicationServices.Application;

#endregion

namespace Gltf2Revit
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public const double m_to_foot = 1000.0 / 304.8;
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

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "choose a gltf file";
            dlg.CheckFileExists = true;
            dlg.Filter = "gltf file|*.glb;*.gltf";
            dlg.RestoreDirectory = true;
            if (dlg.ShowDialog() != DialogResult.OK)
                return Result.Cancelled;

            var gltfModel = ModelRoot.Load(dlg.FileName);

            List<ElementId> matIds = CreateRevitMaterials(doc, gltfModel);

            List<XYZ> loopVertices = new List<XYZ>(3);

            for (int i = 0; i < gltfModel.LogicalMeshes.Count; i++)
            {
                var mesh = gltfModel.LogicalMeshes[i];

                TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                builder.OpenConnectedFaceSet(false);

                for (int j = 0; j < mesh.Primitives.Count; j++)
                {
                    var primitive = mesh.Primitives[j];
                    var vertices = primitive.GetVertices("POSITION").AsVector3Array();
                    var indices = primitive.GetIndices();

                    ElementId matId = matIds[primitive.Material.LogicalIndex];

                    for (int k = 0; k < indices.Count; k+=3)
                    {
                        loopVertices.Clear();
                        loopVertices.Add(new XYZ(
                            -vertices[(int)indices[k]].X * m_to_foot, 
                            vertices[(int)indices[k]].Z * m_to_foot,
                            vertices[(int)indices[k]].Y * m_to_foot));
                        loopVertices.Add(new XYZ(
                            -vertices[(int)indices[k + 1]].X * m_to_foot, 
                            vertices[(int)indices[k + 1]].Z * m_to_foot,
                            vertices[(int)indices[k + 1]].Y * m_to_foot));
                        loopVertices.Add(new XYZ(
                            -vertices[(int)indices[k + 2]].X * m_to_foot, 
                            vertices[(int)indices[k + 2]].Z * m_to_foot,
                            vertices[(int)indices[k + 2]].Y * m_to_foot));

                        var tesseFace = new TessellatedFace(loopVertices, matId);
                        if (builder.DoesFaceHaveEnoughLoopsAndVertices(tesseFace))
                        {
                            builder.AddFace(tesseFace);
                        }
                    }
                }

                builder.CloseConnectedFaceSet();
                builder.Target = TessellatedShapeBuilderTarget.AnyGeometry;
                builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
                builder.Build();

                // Modify document within a transaction
                using (Transaction tx = new Transaction(doc))
                {
                    tx.Start("insert one mesh");
                    
                    DirectShape ds = DirectShape.CreateElement(doc, 
                        new ElementId(BuiltInCategory.OST_GenericModel));
                    ds.ApplicationId = Assembly.GetExecutingAssembly().GetType().GUID.ToString();
                    ds.ApplicationDataId = Guid.NewGuid().ToString();
                    TessellatedShapeBuilderResult result = builder.GetBuildResult();
                    ds.SetShape(result.GetGeometricalObjects());
                    ds.Name = "importedMesh";

                    tx.Commit();
                }
            }

            return Result.Succeeded;
        }

        
        private List<ElementId> CreateRevitMaterials(Document doc, ModelRoot gltfModel)
        {
            List<ElementId> matIds = new List<ElementId>();
            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Create Materials");
                for (int i = 0; i < gltfModel.LogicalMaterials.Count; i++)
                {
                    var mat = gltfModel.LogicalMaterials[i];
                    matIds.Add(CreateRevitMaterial(doc, mat));
                }
                tx.Commit();
            }
            return matIds;
        }

        private ElementId CreateRevitMaterial(Document doc, SharpGLTF.Schema2.Material mat)
        {
            ElementId matId = ElementId.InvalidElementId;
            if (mat == null)
                return matId;

            // Create a new material
            try
            {
                matId = Autodesk.Revit.DB.Material.Create(doc,
                    RevitUtil.GetValidName(mat.Name));
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // material name already exists
                matId = Autodesk.Revit.DB.Material.Create(doc,
                    Guid.NewGuid().ToString());
            }
            
            // Set the material's color
            MaterialChannel? baseColor = mat.FindChannel("BaseColor");
            if (baseColor == null)
                return ElementId.InvalidElementId;
            var rgba = (Vector4)baseColor.Value.Parameters[0].Value;
            float red = rgba.X;
            float green = rgba.Y;
            float blue = rgba.Z;
            float opacity = rgba.W;
            Color color = new Color(
                (byte)(red * 255.0f), 
                (byte)(green * 255.0f), 
                (byte)(blue * 255.0f));
            var material = doc.GetElement(matId) as Autodesk.Revit.DB.Material;
            material.Color = color;
            // Set the material's transparency
            material.Transparency = (int)(1 - opacity) * 100;

            return matId;
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
                result = null;
            }
            return result;
        }
    }
}
