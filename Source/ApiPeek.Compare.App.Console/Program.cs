using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using ApiPeek.Compare.App;


string folder = "api.desktop";
string path1 = "win11.27686";
string path2 = "win11.27686";

ExtractFiles(folder, path1);
ExtractFiles(folder, path2);
MergeAndCompare(false, folder, path1, folder, path2);
MergeAndCompare(true, folder, path1, folder, path2);

string path11 = "win11.26100";
ExtractFiles(folder, path11);
MergeAndCompare(false, folder, path11, folder, path2, "win11.24H2.to.win11.25H2.diff");
MergeAndCompare(true,  folder, path11, folder, path2, "win11.24H2.to.win11.25H2.fulldiff");


static void ExtractFiles(string folder1, string path1)
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

#pragma warning disable CS8321 // Local function is declared but never used
static void ZipAll(string folder1)
{
    string[] dirs = Directory.GetDirectories(folder1);
    foreach (string dir in dirs)
    {
        string dirname = dir.Split(new[] { "\\" }, StringSplitOptions.RemoveEmptyEntries)[1];
        ZipFile.CreateFromDirectory(dir, $"{dirname}.zip", CompressionLevel.Optimal, false, Encoding.UTF8);
    }
}

static void MergeAndCompare(bool detailed, string folder1, string path1, string folder2, string path2, string? fileName = null)
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
    fileName ??= $"{path1}.to.{path2}.{(ApiComparerHtml.DetailedDetailLog ? "full" : "")}diff";
    string pathDiff = $"html\\{fileName}.html";
    ApiComparerHtml.Compare(folder1Files, folder2Files, pathDiff, fileName);
}