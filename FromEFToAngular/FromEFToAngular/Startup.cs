using System;
using System.Collections.Generic;
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
            string srcFilePath = "";

            //TODO: check path and file extension
            if (string.IsNullOrEmpty(srcFilePath))
            {
                Console.WriteLine("Error: edmx file not found.");
                return;
            }

            //Load xml
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(srcFilePath);

            //find entitytype nodes
            List<XmlNode> nodes = new List<XmlNode>();
            RecursiveNodeSearch(xdoc, ref nodes);

            string finalFilePath = "";

            foreach (XmlNode node in nodes)
            {
                GenerateModelFile(node, finalFilePath);
            }
        }

        private static void RecursiveNodeSearch(XmlNode xDoc, ref List<XmlNode> nodes)
        {
            if (xDoc.HasChildNodes)
            {
                foreach (XmlNode node in xDoc.ChildNodes)
                {
                    if (node.Name == "EntityType")
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
                        case "int":
                        case "bigint":
                        case "decimal":
                        case "numeric":
                            line += "number";
                            break;
                        case "bit":
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

            System.IO.File.WriteAllLines(@"" + finalFilepath + node.Attributes["Name"].Value + ".cs", lines);
        }
    }
}
