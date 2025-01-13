using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonalWebApi.Tests
{
    public static class Output
    {
        public static void Write(string message)
        {
            string fancySign = new string('*', 50);
            Debug.WriteLine($"{fancySign}\n\t#### MyLog| {message}");
            Console.WriteLine($"{fancySign}\n\t#### MyLog| {message}");
        }
    }
}
