using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace P3_Producer
{
    internal class InputReader
    {
        public static (int, int, int) ReadInput(string filePath)
        {
            int p = 0, c = 0, q = 0;
            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("p="))
                    p = int.Parse(line.Substring(2).Trim());
                else if (line.StartsWith("c="))
                    c = int.Parse(line.Substring(2).Trim());
                else if (line.StartsWith("q="))
                    q = int.Parse(line.Substring(2).Trim());
            }
            return (p, c, q);
        }
    }
}
