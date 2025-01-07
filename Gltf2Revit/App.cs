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

            string tabName = "以见测试";
            try
            {
                a.CreateRibbonTab(tabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Do nothing.
            }

            // Add a new ribbon panel
            RibbonPanel newPanel = a.CreateRibbonPanel(tabName, "测试面板");

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData buttonData = new PushButtonData("gltf2revit",
                "gltf2revit", thisAssemblyPath, "Gltf2Revit.Command");
            PushButton pushButton = newPanel.AddItem(buttonData) as PushButton;
            pushButton.LargeImage = BmpImageSource(@"Gltf2Revit.Resources.icons8-scroll-3296.png");

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
