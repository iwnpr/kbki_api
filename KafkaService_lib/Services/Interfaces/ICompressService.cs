using System.IO.Compression;

namespace KafkaService_lib.Services.Interfaces
{
    public interface ICompressService
    {
        string Compress(string uncompressedString, CompressionLevel compressionLevel = CompressionLevel.Fastest);
        string Decompress(string compressedString);
    }
}
