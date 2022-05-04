using System.Security.Cryptography;
using System.Text;

namespace ApiPeek.Compare.App;

public static class HashUtils
{
    private static readonly MD5 Md5 = MD5.Create();

    /// <summary>
    /// Compute hash for string encoded as UTF8
    /// </summary>
    /// <param name="s">String to be hashed</param>
    /// <returns>32-character hex string</returns>
    public static string ToHash(this string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        byte[] hashBytes = Md5.ComputeHash(bytes);
        return HexStringFromBytes(hashBytes);
    }

    /// <summary>
    /// Convert an array of bytes to a string of hex digits
    /// </summary>
    /// <param name="bytes">array of bytes</param>
    /// <returns>String of hex digits</returns>
    public static string HexStringFromBytes(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}