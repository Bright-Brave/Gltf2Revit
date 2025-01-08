using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gltf2Revit
{
    public static class RevitUtil
    {
        public static string GetValidName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return Guid.NewGuid().ToString();
            else return name
                .Replace("\\","/")
                .Replace(":","-")
                .Replace("{", "-")
                .Replace("}", "-")
                .Replace("[", "-")
                .Replace("]", "-")
                .Replace(";", " ")
                .Replace("<", "-")
                .Replace(">", "-")
                .Replace("?", "-")
                .Replace("`", "-")
                .Replace("~", "-");
        }
    }
}
