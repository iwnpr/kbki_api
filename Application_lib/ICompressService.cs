using System.IO.Compression;

namespace Application_lib
{
    public interface ICompressService
    {
        string Compress(byte[] uncompressedString, CompressionLevel compressionLevel = CompressionLevel.Fastest);
        string Decompress(string compressedString);
    }
}
