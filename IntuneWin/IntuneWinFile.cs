using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using IntuneWin.Data;
using IntuneWin.Exceptions;

namespace IntuneWin
{
    /// <summary>
    /// IntuneWin file handler.
    /// </summary>
    public class IntuneWinFile : IDisposable
    {
        /// <summary>
        /// Path to the metadata XML file within IntuneWin file.
        /// </summary>
        private const string MetaDataFilePath = "IntuneWinPackage/Metadata/Detection.xml";
        
        /// <summary>
        /// Path to the content file withing IntuneWin file.
        /// </summary>
        private string ContentFilePath => $"IntuneWinPackage/Contents/{ApplicationInfo.FileName}";

        private static readonly XmlSerializer MetaDataSerializer = new XmlSerializer(typeof(ApplicationInfo));

        private readonly ZipArchive _fileArchive;
        private readonly Stream _fileStream;
        
        /// <summary>
        /// Application metadata.
        /// </summary>
        public ApplicationInfo ApplicationInfo { get; }

        /// <summary>
        /// Create new file.
        /// </summary>
        /// <param name="filePath">Path to save the file.</param>
        /// <param name="name">Package name.</param>
        /// <param name="description">Package description.</param>
        /// <param name="fileName">Inner content file name. I.e. my-application.intunewin.</param>
        /// <param name="setupFile">Setup file name within the content file.</param>
        public IntuneWinFile(string filePath, string name, string description, string fileName, string setupFile)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            _fileStream = File.Open(filePath, FileMode.Create, FileAccess.ReadWrite);

            _fileArchive = new ZipArchive(
                _fileStream, ZipArchiveMode.Update, true, Encoding.UTF8);

            ApplicationInfo = new ApplicationInfo
            {
                Name = name,
                Description = description,
                FileName = fileName,
                SetupFile = setupFile,
                EncryptionInfo = new FileEncryptionInfo
                {
                    EncryptionKey = Convert.ToBase64String(GenerateKey()),
                    MacKey = Convert.ToBase64String(GenerateKey()),
                    InitializationVector = Convert.ToBase64String(GenerateIv())
                }
            };

            SaveMetaData();
        }

        /// <summary>
        /// Open existing file via path.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        public IntuneWinFile(string filePath) : this(File.Open(filePath, FileMode.Open, FileAccess.ReadWrite))
        {
        }

        /// <summary>
        /// Open existing file from stream.
        /// </summary>
        /// <param name="fileStream">Seekable and readable stream.</param>
        public IntuneWinFile(Stream fileStream)
        {
            _fileStream = fileStream;
            
            try
            {
                _fileArchive = new ZipArchive(fileStream, ZipArchiveMode.Update, true, Encoding.UTF8);

                if (_fileArchive.GetEntry(MetaDataFilePath) == null)
                    throw new IntuneWinInvalidFileException(
                        new FileNotFoundException("Metadata entry is not present in file", MetaDataFilePath));

                ApplicationInfo = (ApplicationInfo) MetaDataSerializer.Deserialize(
                    _fileArchive.GetEntry(MetaDataFilePath)?.Open() ?? throw new IntuneWinInvalidFileException());
            }
            catch (Exception exception)
            {
                throw new IntuneWinInvalidFileException(exception);
            }
        }
        
        /// <summary>
        /// Close and dispose file stream and archive.
        /// </summary>
        public void Dispose()
        {
            _fileArchive?.Dispose();
            _fileStream?.Dispose();
        }

        /// <summary>
        /// Add content stream to the IntuneWin file.
        /// </summary>
        /// <param name="fileStream">Seekable and readable stream.</param>
        public async Task EmbedContentFileAsync(Stream fileStream)
        {
            var tempFile = Path.GetTempFileName();

            try
            {
                ApplicationInfo.UnencryptedContentSize = fileStream.Length;
                
                ApplicationInfo.EncryptionInfo.FileDigest = Convert.ToBase64String(
                    Sha256WithBufferSize(fileStream));

                using var tempStream = File.Open(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                await EncryptStream(fileStream, tempStream);
                
                _fileArchive.GetEntry(ContentFilePath)?.Delete();
                _fileArchive.CreateEntryFromFile(tempFile, ContentFilePath);
            }
            finally
            {
                SaveMetaData();

                File.Delete(tempFile);
            }
        }
        
        /// <summary>
        /// Add content file to the IntuneWin file.
        /// </summary>
        /// <param name="filePath">Content file path.</param>
        public async Task EmbedContentFileAsync(string filePath)
        {
            await EmbedContentFileAsync(File.OpenRead(filePath));
        }
        
        /// <summary>
        /// Extract content file from the IntuneWin file.
        /// </summary>
        /// <param name="filePath">Path where the file should be save.</param>
        public async Task ExtractContentFileAsync(string filePath)
        {
            await ExtractContentFileAsync(
                File.Open(filePath, FileMode.Create, FileAccess.ReadWrite));
        }

        /// <summary>
        /// Extract content file from the IntuneWin file.
        /// </summary>
        /// <param name="fileStream">Stream to which content file should be extracted.</param>
        public async Task ExtractContentFileAsync(Stream fileStream)
        {
            using (fileStream)
            {
                var contentFileEntry = _fileArchive.GetEntry(ContentFilePath);

                if (contentFileEntry == null)
                    throw new IntuneWinInvalidFileException(
                        new FileNotFoundException("Content entry is not present in file", ContentFilePath));

                using var contentFileStream = contentFileEntry.Open();

                await DecryptStream(contentFileStream, fileStream);
            }
        }

        private async Task EncryptStream(Stream input, Stream output)
        {
            var buffer = new byte[2097152];

            if (string.IsNullOrEmpty(ApplicationInfo.EncryptionInfo.EncryptionKey) ||
                string.IsNullOrEmpty(ApplicationInfo.EncryptionInfo.InitializationVector) ||
                string.IsNullOrEmpty(ApplicationInfo.EncryptionInfo.MacKey))
                throw new IntuneWinInvalidFileException();

            var encryptionKey = Convert.FromBase64String(
                ApplicationInfo.EncryptionInfo.EncryptionKey);

            var encryptionIv = Convert.FromBase64String(
                ApplicationInfo.EncryptionInfo.InitializationVector);

            var macKey = Convert.FromBase64String(
                ApplicationInfo.EncryptionInfo.MacKey);

            await output.WriteAsync(buffer, 0, 48);

            using var aes = Aes.Create();

            if (aes == null)
                throw new InvalidOperationException();

            using var cryptoTransform = aes.CreateEncryptor(encryptionKey, encryptionIv);
            using var cryptoStream = new CryptoStream(output, cryptoTransform, CryptoStreamMode.Write);

            int count;

            while ((count = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await cryptoStream.WriteAsync(buffer, 0, count);
                await cryptoStream.FlushAsync();
            }

            cryptoStream.FlushFinalBlock();

            output.Seek(32, SeekOrigin.Begin);
            await output.WriteAsync(encryptionIv, 0, encryptionIv.Length);
            output.Seek(32, SeekOrigin.Begin);

            var macHash = HmacSha256WithBufferSize(output, macKey);

            output.Seek(0, SeekOrigin.Begin);
            await output.WriteAsync(macHash, 0, macHash.Length);

            ApplicationInfo.EncryptionInfo.Mac = Convert.ToBase64String(macHash);

            await output.FlushAsync();
        }
        
        private static byte[] Sha256WithBufferSize(Stream input, int bufferSize = 2097152)
        {
            try
            {
                using var sha256 = SHA256.Create();

                if (sha256 == null)
                    throw new InvalidOperationException();

                var buffer = new byte[bufferSize];

                int count;

                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                    sha256.TransformBlock(buffer, 0, count, buffer, 0);

                sha256.TransformFinalBlock(buffer, 0, 0);

                return sha256.Hash;
            }
            finally
            {
                input.Seek(0, SeekOrigin.Begin);
            }
        }

        private static byte[] HmacSha256WithBufferSize(Stream input, byte[] key, int bufferSize = 2097152)
        {
            try
            {
                using var hmac = KeyedHashAlgorithm.Create("HMACSHA256");

                if (hmac == null)
                    throw new InvalidOperationException();

                hmac.Key = key;

                var buffer = new byte[bufferSize];

                int count;

                while ((count = input.Read(buffer, 0, buffer.Length)) > 0)
                    hmac.TransformBlock(buffer, 0, count, buffer, 0);

                hmac.TransformFinalBlock(buffer, 0, 0);

                return hmac.Hash;
            }
            finally
            {
                input.Seek(0, SeekOrigin.Begin);
            }
        }

        private void SaveMetaData()
        {
            _fileArchive.GetEntry(MetaDataFilePath)?.Delete();
            
            var metaDataEntry = _fileArchive.CreateEntry(MetaDataFilePath);

            var metaDataEntryStream = metaDataEntry.Open();

            MetaDataSerializer.Serialize(metaDataEntryStream, ApplicationInfo);

            metaDataEntryStream.Flush();
            metaDataEntryStream.Close();
        }

        private static byte[] GenerateKey()
        {
            using var cryptoServiceProvider = new AesCryptoServiceProvider();
            cryptoServiceProvider.GenerateKey();

            return cryptoServiceProvider.Key;
        }

        private static byte[] GenerateIv()
        {
            using var aes = Aes.Create();

            if (aes == null)
                throw new InvalidOperationException();

            return aes.IV;
        }

        private async Task DecryptStream(Stream input, Stream output)
        {
            var buffer = new byte[2097152];

            if (string.IsNullOrEmpty(ApplicationInfo.EncryptionInfo.EncryptionKey) ||
                string.IsNullOrEmpty(ApplicationInfo.EncryptionInfo.InitializationVector))
                throw new IntuneWinInvalidFileException();

            var encryptionKey = Convert.FromBase64String(
                ApplicationInfo.EncryptionInfo.EncryptionKey);

            var encryptionIv = Convert.FromBase64String(
                ApplicationInfo.EncryptionInfo.InitializationVector);

            using var aes = Aes.Create();

            if (aes == null)
                throw new InvalidOperationException();

            // skip HMAC signature and IV
            input.Seek(48, SeekOrigin.Begin);

            using var cryptoTransform = aes.CreateDecryptor(encryptionKey, encryptionIv);
            using var cryptoStream = new CryptoStream(output, cryptoTransform, CryptoStreamMode.Write);

            int count;

            while ((count = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await cryptoStream.WriteAsync(buffer, 0, count);
                await cryptoStream.FlushAsync();
            }

            cryptoStream.FlushFinalBlock();
        }
    }
}