using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace FunctionClassroom
{
    [XmlRoot("FunctionWorkspace")]
    public sealed class SavedWorkspace
    {
        public SavedWorkspace()
        {
            XMin = -10.0;
            XMax = 10.0;
            YMin = -7.0;
            YMax = 7.0;
            Functions = new List<SavedFunction>();
        }

        public double XMin { get; set; }
        public double XMax { get; set; }
        public double YMin { get; set; }
        public double YMax { get; set; }

        [XmlArray("Functions")]
        [XmlArrayItem("Function")]
        public List<SavedFunction> Functions { get; set; }
    }

    public sealed class SavedFunction
    {
        public SavedFunction()
        {
            IsVisible = true;
            Color = "#1565C0";
            ParameterNames = new List<string>();
            ParameterValues = new List<double>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Expression { get; set; }
        public string Color { get; set; }
        public bool IsVisible { get; set; }
        public List<string> ParameterNames { get; set; }
        public List<double> ParameterValues { get; set; }
    }

    public static class ProjectStore
    {
        public static string DefaultPath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "FunctionClassroom");
                return Path.Combine(folder, "workspace.xml");
            }
        }

        public static void Save(string path, SavedWorkspace workspace)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A save path is required.", "path");
            if (workspace == null)
                throw new ArgumentNullException("workspace");

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temporary = path + ".tmp";
            XmlSerializer serializer = new XmlSerializer(typeof(SavedWorkspace));
            using (FileStream stream = new FileStream(
                temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, workspace);
            }

            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }

        public static SavedWorkspace Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return new SavedWorkspace();
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SavedWorkspace));
                using (FileStream stream = new FileStream(
                    path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    SavedWorkspace workspace = serializer.Deserialize(stream) as SavedWorkspace;
                    return workspace ?? new SavedWorkspace();
                }
            }
            catch
            {
                return new SavedWorkspace();
            }
        }
    }
}
