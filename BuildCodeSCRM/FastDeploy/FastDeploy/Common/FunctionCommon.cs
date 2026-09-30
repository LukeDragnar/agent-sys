using FastDeploy.Models;
using System.Xml.Linq;

namespace FastDeploy.Common
{
    public class FunctionCommon
    {
        public static string UpVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return version;

            var parts = version.Split('.');

            int lastIndex = parts.Length - 1;

            // độ dài ban đầu của phần cuối (vd: "01" => 2)
            int length = parts[lastIndex].Length;

            if (!int.TryParse(parts[lastIndex], out int number))
                return version;

            number++;

            // giữ padding số 0
            parts[lastIndex] = number.ToString().PadLeft(length, '0');

            return string.Join(".", parts);
        }
    }
}
