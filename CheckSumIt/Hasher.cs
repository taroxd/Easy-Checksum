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
        readonly private static uint BUFFER_SIZE;
        private const uint MB = 1024 * 1024;

        static Hasher()
        {
            ulong memoryAvailable = MemoryManager.AppMemoryUsageLimit - MemoryManager.AppMemoryUsage;

            if (memoryAvailable >= (ulong)2048 * MB)
            {
                BUFFER_SIZE = 1024 * MB;
            }
            else
            {
                int tryCapacity = (int)(memoryAvailable / 2) - 10 * (int)MB;
                BUFFER_SIZE = tryCapacity > 1 * MB ? (uint)tryCapacity : 1 * MB;
            }
        }

        public async static Task<IEnumerable<string>> GetHashAsync(IStorageFile file, IEnumerable<string> hashAlgorithms)
        {
            var fileProperties = await file.GetBasicPropertiesAsync();
            var size = fileProperties.Size;
            
            // small file, store in the memory
            if (size <= BUFFER_SIZE)
            {
                var buffer = await FileIO.ReadBufferAsync(file);
                return hashAlgorithms.Select(algorithm =>
                {
                    var cryptoHash = AlgorithmToHash(algorithm);
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
                    var buffer = new Windows.Storage.Streams.Buffer(BUFFER_SIZE);

                    var cryptoHashes = hashAlgorithms.Select(AlgorithmToHash);

                    while (true)
                    {
                        await inputStream.ReadAsync(buffer, BUFFER_SIZE, InputStreamOptions.None);
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

        private static CryptographicHash AlgorithmToHash(string algorithm)
        {
            return HashAlgorithmProvider.OpenAlgorithm(algorithm).CreateHash();
        }
    }
}
