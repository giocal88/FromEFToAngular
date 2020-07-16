﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace FromEFToAngular
{
    class Startup
    {
        static void Main(string[] args)
        {
            string sCurrentDir = Directory.GetCurrentDirectory();
            string srcFilePath = "";
            string sfinalFilePath = "";

            //read conf
            string[] lines = File.ReadAllLines(Path.Combine(sCurrentDir, "conf.txt"));
            foreach (string line in lines)
            {
                string key = line.Split("=")[0];
                string value = line.Split("=")[1];

                switch (key)
                {
                    case "edmx":
                        srcFilePath = value;
                        break;
                    case "dest":
                        sfinalFilePath = value;
                        break;
                }
                    
            }

            //TODO: check path and file extension
            if (string.IsNullOrEmpty(srcFilePath))
            {
                Console.WriteLine("Error: edmx file not found.");
                return;
            }

            //eventually delete old files
            string[] filePaths = Directory.GetFiles(sfinalFilePath);
            foreach (string filePath in filePaths)
                File.Delete(filePath);

            //Load xml
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(srcFilePath);

            //find entitytype nodes
            List<XmlNode> nodes = new List<XmlNode>();
            RecursiveNodeSearch(xdoc, ref nodes);

            foreach (XmlNode node in nodes)
            {
                GenerateModelFile(node, sfinalFilePath);
            }
        }

        private static void RecursiveNodeSearch(XmlNode xDoc, ref List<XmlNode> nodes)
        {
            if (xDoc.HasChildNodes)
            {
                foreach (XmlNode node in xDoc.ChildNodes)
                {
                    if (node.Name == "EntityType" || node.Name == "ComplexType")
                        nodes.Add(node);
                    else
                        RecursiveNodeSearch(node, ref nodes);
                }
            }
        }

        private static void GenerateModelFile(XmlNode node, string finalFilepath)
        { 
            List<string> lines = new List<string>();

            //class
            string line = $"export class " + node.Attributes["Name"].Value + " {";
            lines.Add(line);            

            //object properties
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "Property")
                {
                    line = "    public " + childNode.Attributes["Name"].Value + ": ";
                    switch (childNode.Attributes["Type"].Value)
                    {
                        case "Int16":
                        case "Int32":
                        case "Decimal":
                        case "Double":
                        case "Single":
                        case "long":
                            line += "number";
                            break;
                        case "Boolean":
                            line += "boolean";
                            break;
                        default:
                            line += "string";
                            break;
                    }

                    line += ";";
                    lines.Add(line);
                }
            }

            //constructor
            line = "    constructor() {}";
            lines.Add(line);

            //end
            line = "}";
            lines.Add(line);

            System.IO.File.WriteAllLines(@"" + Path.Combine(finalFilepath, node.Attributes["Name"].Value + ".ts"), lines);
        }
    }
}
