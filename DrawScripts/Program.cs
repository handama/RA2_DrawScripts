using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DrawScripts
{
    class Program
    {
        public static string MAP_NAME;
        public static IniFile MAP_FILE;
       
        static void Main(string[] args)
        {
            Console.WriteLine("Please enter map file name:");
            MAP_NAME = Console.ReadLine();
            MAP_FILE = new IniFile(MAP_NAME);
            var map = new MapFile();
            map.ReadIsoMapPack5(MAP_NAME);
            map.CreateRadarColor();
            map.ReadWP();
            map.ReadScript();
            map.CreateBitMapbyMap(MAP_NAME+"_Waypoints");
        }
    }
}
