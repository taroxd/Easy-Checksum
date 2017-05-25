using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using System.IO;
using Windows.System;

namespace CheckSumIt
{
    static class Hasher
    {
        readonly private static uint CAPACITY;
        private const uint MB = 1024 * 1024;

        static private readonly Dictionary<string, CryptographicHash> nameToHasher = new Dictionary<string, CryptographicHash>
        {
            { "MD5", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash() },
            { "SHA-1", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1).CreateHash() },
            { "SHA-256", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256).CreateHash() },
            { "SHA-384", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha384).CreateHash() },
            { "SHA-512", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512).CreateHash() }
        };

        static Hasher()
        {
            ulong memoryAvailable = MemoryManager.AppMemoryUsageLimit - MemoryManager.AppMemoryUsage;

            if (memoryAvailable >= (ulong)2048 * MB)
            {
                CAPACITY = 1024 * MB;
            }
            else
            {
                int tryCapacity = (int)(memoryAvailable / 2) - 10 * (int)MB;
                CAPACITY = tryCapacity > 1 * MB ? (uint)tryCapacity : 1 * MB;
            }
        }

        public async static Task<IEnumerable<string>> GetHashAsync(IStorageFile file, IEnumerable<string> hashAlgorithms)
        {
            var fileProperties = await file.GetBasicPropertiesAsync();
            var size = fileProperties.Size;
            
            // small file, store in the memory
            if (size <= CAPACITY)
            {
                var buffer = await FileIO.ReadBufferAsync(file);
                return hashAlgorithms.Select(hashName =>
                {
                    var cryptoHash = nameToHasher[hashName];
                    cryptoHash.Append(buffer);
                    return CryptographicBuffer.EncodeToHexString(cryptoHash.GetValueAndReset());
                });
            }
            // big file
            else
            {
                using (var stream = await file.OpenStreamForReadAsync())
                using (var inputStream = stream.AsInputStream())
                {
                    var buffer = new Windows.Storage.Streams.Buffer(CAPACITY);

                    var cryptoHashes = from hashName in hashAlgorithms select nameToHasher[hashName];

                    while (true)
                    {
                        await inputStream.ReadAsync(buffer, CAPACITY, InputStreamOptions.None);
                        if (buffer.Length > 0)
                            foreach(var cryptoHash in cryptoHashes)
                            {
                                cryptoHash.Append(buffer);
                            }
                        else
                            break;
                    }
                    return (from cryptoHash in cryptoHashes
                            select CryptographicBuffer.EncodeToHexString(cryptoHash.GetValueAndReset()));
                }
            }
        }

    }
}
