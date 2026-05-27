using System;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

class Program
{
    static void Main()
    {
        var methods = typeof(Assert).GetMethods();
        foreach (var m in methods)
        {
            if (m.Name.Contains("Throw"))
            {
                Console.WriteLine($"{m.Name} - {m.ToString()}");
            }
        }
    }
}
