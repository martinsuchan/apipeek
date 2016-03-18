using System.IO;
using System.Linq;

namespace ApiPeek.Compare.App
{
    class Program
    {
        static void Main(string[] args)
        {
            //string path1 = "band.1.3.10929";
            //string path2 = "band.1.3.11121";

            string path1 = "win10.14291";
            string path2 = "win10.14291";

            //string path1 = "wp10.14291";
            //string path2 = "wp10.14291";

            MergeAndCompare(false, path1, path2);
            MergeAndCompare(true, path1, path2);

            //Console.ReadKey();
        }

        private static void SortAndCompare(string folder1, string folder2, params string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0)
            {
                string[] fileNamesOld = Directory.GetFiles($"api\\{folder1}")
                    .Where(f => f.EndsWith(".json"))
                    .Select(f => f.Split('\\').Last())
                    .ToArray();
                string[] fileNamesNew = Directory.GetFiles($"api\\{folder2}")
                    .Where(f => f.EndsWith(".json"))
                    .Select(f => f.Split('\\').Last())
                    .ToArray();
                fileNames = fileNamesOld.Concat(fileNamesNew).Distinct().ToArray();
            }

            foreach (string fileName in fileNames)
            {
                string path1 = $"api\\{folder1}\\{fileName}";
                string path2 = $"api\\{folder2}\\{fileName}";
                string pathDiff = $"api\\{folder2}\\diff-{fileName}.txt";

                ApiComparerTxt.Compare(path1, path2, pathDiff);
            }
        }

        private static void MergeAndCompare(bool detailed, string folder1, string folder2, params string[] fileNames)
        {
            if (fileNames == null || fileNames.Length == 0)
            {
                string[] fileNamesOld = Directory.GetFiles($"api\\{folder1}")
                    .Where(f => f.EndsWith(".json"))
                    .Select(f => f.Split('\\').Last())
                    .ToArray();
                string[] fileNamesNew = Directory.GetFiles($"api\\{folder2}")
                    .Where(f => f.EndsWith(".json"))
                    .Select(f => f.Split('\\').Last())
                    .ToArray();
                fileNames = fileNamesOld.Concat(fileNamesNew).Distinct().ToArray();
            }

            ApiComparerHtml.DetailedDetailLog = detailed;
            string[] folder1Files = fileNames.Select(f => $"api\\{folder1}\\{f}").ToArray();
            string[] folder2Files = fileNames.Select(f => $"api\\{folder2}\\{f}").ToArray();
            string fileName = $"{folder1}.to.{folder2}.{(ApiComparerHtml.DetailedDetailLog ? "full" : "")}diff";
            string pathDiff = $"html\\{fileName}.html";
            ApiComparerHtml.Compare(folder1Files, folder2Files, pathDiff, fileName);
        }
    }
}