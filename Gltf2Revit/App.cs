#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#endregion

namespace Gltf2Revit
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            ControlledApplication app = a.ControlledApplication;

            RibbonPanel newPanel = null;
            try
            {
                newPanel = a.CreateRibbonPanel("Import Tools");
            }
            catch (System.Exception)
            {
                newPanel = a.GetRibbonPanels().Find(x => x.Name == "Import Tools");
            }

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("ImportGltf",
                "ImportGltf", thisAssemblyPath, "Gltf2Revit.Command");
            PushButton pushButton = newPanel.AddItem(buttonData) as PushButton;
            pushButton.LargeImage = BmpImageSource(@"Gltf2Revit.Resources.importGltf.png");

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            ControlledApplication app = a.ControlledApplication;
            return Result.Succeeded;
        }

        private ImageSource BmpImageSource(string embeddedPath)
        {
            System.IO.Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedPath);
            PngBitmapDecoder pngBitmapDecoder = new PngBitmapDecoder(manifestResourceStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return pngBitmapDecoder.Frames[0];
        }
    }
}
