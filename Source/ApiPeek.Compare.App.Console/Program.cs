﻿using System.IO;
using System.Linq;

namespace ApiPeek.Compare.App
{
    class Program
    {
        static void Main(string[] args)
        {
            string folder = "api.desktop";
            string path1 = "win10.17763";
            string path2 = "win10.17763";

            MergeAndCompare(false, folder, path1, folder, path2);
            MergeAndCompare(true, folder, path1, folder, path2);

            string path1809 = "win10.17763";
            MergeAndCompare(false, folder, path1809, folder, path2, "win10.1809.to.win10.1903.diff");
            MergeAndCompare(true, folder, path1809, folder, path2, "win10.1809.to.win10.1903.fulldiff");
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