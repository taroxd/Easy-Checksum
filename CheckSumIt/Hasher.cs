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

namespace CheckSumIt
{
    static class Hasher
    {
        const int CAPACITY = 1024 * 1024 * 256;

        static private readonly Dictionary<string, CryptographicHash> nameToHasher = new Dictionary<string, CryptographicHash>
        {
            { "MD5", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash() },
            { "SHA-1", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1).CreateHash() },
            { "SHA-256", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256).CreateHash() },
            { "SHA-384", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha384).CreateHash() },
            { "SHA-512", HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512).CreateHash() }
        };

        public async static Task<IEnumerable<string>> GetHashAsync(IStorageFile file, IEnumerable<string> hashAlgorithms)
        {
            var fileProperties = await file.GetBasicPropertiesAsync();
            var size = fileProperties.Size;
            
            // small file, store in the memory
            if (size < CAPACITY)
            {
                var buffer = await Windows.Storage.FileIO.ReadBufferAsync(file);
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
