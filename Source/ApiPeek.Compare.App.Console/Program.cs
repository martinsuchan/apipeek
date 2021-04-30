using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace ApiPeek.Compare.App
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = "api.desktop";
            string path1 = "win10.21370";
            string path2 = "win10.21370";

            ExtractFiles(folder, path1);
            ExtractFiles(folder, path2);
            MergeAndCompare(false, folder, path1, folder, path2);
            MergeAndCompare(true, folder, path1, folder, path2);

            string path21H1 = "win10.19043";
            ExtractFiles(folder, path21H1);
            MergeAndCompare(false, folder, path21H1, folder, path2, "win10.21H1.to.win10.21H2.diff");
            MergeAndCompare(true,  folder, path21H1, folder, path2, "win10.21H1.to.win10.21H2.fulldiff");
        }

        private static void ExtractFiles(string folder1, string path1)
        {
            string dir1Path = $"{folder1}\\{path1}";
            string zip1Path = $"{dir1Path}.zip";
            Debug.Assert(File.Exists(zip1Path));

            if (Directory.Exists(dir1Path))
            {
                Directory.Delete(dir1Path, true);
            }
            ZipFile.ExtractToDirectory(zip1Path, dir1Path);
        }

        private static void ZipAll(string folder1)
        {
            string[] dirs = Directory.GetDirectories(folder1);
            foreach (string dir in dirs)
            {
                string dirname = dir.Split(new[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[1];
                ZipFile.CreateFromDirectory(dir, $"{dirname}.zip", CompressionLevel.Optimal, false, Encoding.UTF8);
            }
        }

        private static void MergeAndCompare(bool detailed, string folder1, string path1, string folder2, string path2, string fileName = null)
        {
            string[] fileNamesOld = Directory.GetFiles($"{folder1}\\{path1}")
                .Where(f => f.EndsWith(".json"))
                .Select(f => f.Split('\\').Last())
                .ToArray();
            string[] fileNamesNew = Directory.GetFiles($"{folder2}\\{path2}")
                .Where(f => f.EndsWith(".json"))
                .Select(f => f.Split('\\').Last())
                .ToArray();
            string[] fileNames = fileNamesOld.Concat(fileNamesNew).Distinct().ToArray();

            ApiComparerHtml.DetailedDetailLog = detailed;
            string[] folder1Files = fileNames.Select(f => $"{folder1}\\{path1}\\{f}").ToArray();
            string[] folder2Files = fileNames.Select(f => $"{folder2}\\{path2}\\{f}").ToArray();
            if (fileName == null)
            {
                fileName = $"{path1}.to.{path2}.{(ApiComparerHtml.DetailedDetailLog ? "full" : "")}diff";
            }
            string pathDiff = $"html\\{fileName}.html";
            ApiComparerHtml.Compare(folder1Files, folder2Files, pathDiff, fileName);
        }
    }
}