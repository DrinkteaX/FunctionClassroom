using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FunctionClassroom
{
    public static class ImageExporter
    {
        public static void SavePng(string path, GraphView graph)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("A PNG path is required.", "path");
            if (graph == null)
                throw new ArgumentNullException("graph");

            BitmapSource bitmap = graph.RenderBitmap(2.0);
            if (bitmap == null)
                throw new InvalidOperationException("The graph has no drawable area.");

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                encoder.Save(stream);
        }

        public static void CopyToClipboard(GraphView graph)
        {
            if (graph == null)
                throw new ArgumentNullException("graph");
            BitmapSource bitmap = graph.RenderBitmap(2.0);
            if (bitmap == null)
                throw new InvalidOperationException("The graph has no drawable area.");
            Clipboard.SetImage(bitmap);
        }
    }
}
