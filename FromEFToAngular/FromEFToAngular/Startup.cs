using FromEFToAngular.Model;
using System;
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

            try
            {
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

                //old models cleaning
                FileDeletingManagement(sfinalFilePath);

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
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private static void RecursiveNodeSearch(XmlNode xDoc, ref List<XmlNode> nodes)
        {
            if (xDoc.HasChildNodes)
            {
                foreach (XmlNode node in xDoc.ChildNodes)
                {
                    if (node.ParentNode != null && node.ParentNode.ParentNode != null && 
                        node.ParentNode.ParentNode.Name == "edmx:ConceptualModels" && (node.Name == "EntityType" || node.Name == "ComplexType"))
                        nodes.Add(node);
                    else
                        RecursiveNodeSearch(node, ref nodes);
                }
            }
        }

        private static Entity GenerateModelFile(XmlNode node, string finalFilepath)
        { 
            List<string> lines = new List<string>();
            Entity entity = new Entity();
            List<EntityProperty> entityProperties = new List<EntityProperty>();

            entity.Name = node.Attributes["Name"].Value;

            //class
            string line = $"export class " + entity.Name + " {";
            lines.Add(line);

            //object properties
            List<string> propertiesLines = new List<string>();
            List<string> methodsLines = new List<string>();
            foreach (XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "Property")
                {
                    EntityProperty entityProperty = new EntityProperty();
                    entityProperty.Nmae = childNode.Attributes["Name"].Value;

                    line = "    public _" + entityProperty.Nmae + ": ";
                    switch (childNode.Attributes["Type"].Value)
                    {
                        case "Int16":
                        case "Int32":
                        case "Decimal":
                        case "Double":
                        case "Single":
                        case "long":
                            entityProperty.Type = "number";
                            break;
                        case "Boolean":
                            entityProperty.Type  = "boolean";
                            break;
                        default:
                            entityProperty.Type = "string";
                            break;
                    }

                    entityProperties.Add(entityProperty);

                    line += entityProperty.Type;
                    line += ";";
                    propertiesLines.Add(line);

                    //getter
                    line = "    get " + entityProperty.Nmae + "(): " + entityProperty.Type + " { return this._" + entityProperty.Nmae + "; }";
                    methodsLines.Add(line);

                    //setter
                    line = "    set " + entityProperty.Nmae + "(" + entityProperty.Nmae + ": " + entityProperty.Type + ") { this._" + entityProperty.Nmae + " = " + entityProperty.Nmae + "; }";
                    methodsLines.Add(line);
                }
            }

            entity.Properties = entityProperties;

            lines.AddRange(propertiesLines);

            //space line
            line = Environment.NewLine;
            lines.Add(line);

            lines.AddRange(methodsLines);

            //space line
            line = Environment.NewLine;
            lines.Add(line);

            //constructor
            line = "    constructor() {}";
            lines.Add(line);

            //end
            line = "}";
            lines.Add(line);

            File.WriteAllLines(@"" + Path.Combine(finalFilepath, node.Attributes["Name"].Value + ".ts"), lines);

            return entity;
        }

        private static void FileDeletingManagement(string sfinalFilePath)
        {
            //eventually delete old files
            string[] filePaths = Directory.GetFiles(sfinalFilePath);

            if (filePaths.Length > 0)
            {
                Console.WriteLine("This files are about to be deleted:");
                foreach (string filePath in filePaths)
                {
                    Console.WriteLine(filePath);
                }

                Console.WriteLine("Are you sure you want to delete this files? [y/N]");
                string res = Console.ReadLine();

                if (res.ToLower() == "y")
                {
                    foreach (string filePath in filePaths)
                    {
                        File.Delete(filePath);
                    }
                }
            }

        }
    }
}
