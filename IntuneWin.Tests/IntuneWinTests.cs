using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace IntuneWin.Tests
{
    public class IntuneWinTests
    {
        [Test]
        public void OpenFileCreatedExternally()
        {
            using var file = new IntuneWinFile(@"sample.intunewin");

            Assert.IsNotEmpty(file.ApplicationInfo.Name);
            Assert.IsNotEmpty(file.ApplicationInfo.FileName);
            Assert.IsNotEmpty(file.ApplicationInfo.SetupFile);
            Assert.Greater(file.ApplicationInfo.UnencryptedContentSize, 0);
        }

        [Test]
        public async Task ExtractContentFromFileCreatedExternally()
        {
            using var file = new IntuneWinFile(@"sample.intunewin");

            const string contentFileName = "extracted-sample-content.intunewin";
            
            await file.ExtractContentFileAsync(contentFileName);
            
            Assert.True(File.Exists(contentFileName));
            
            Assert.AreEqual(new FileInfo(contentFileName).Length, 3078);
            
            Assert.True(FilesAreEqual(
                new FileInfo("extracted-sample-content.intunewin"), 
                new FileInfo(@"sample.zip")));
        }

        [Test]
        public async Task CreateNewFile()
        {
            using var file = new IntuneWinFile("created.intunewin", "Lorem", "LoremIpsumDolorSetAmet", 
                "lorem.intunewin", "file.txt");

            await file.EmbedContentFileAsync(@"sample.zip");
            
            Assert.IsNotEmpty(file.ApplicationInfo.Name);
            Assert.IsNotEmpty(file.ApplicationInfo.FileName);
            Assert.IsNotEmpty(file.ApplicationInfo.SetupFile);
            Assert.Greater(file.ApplicationInfo.UnencryptedContentSize, 0);
            
            Assert.AreEqual(file.ApplicationInfo.UnencryptedContentSize, 3078);
            
            Assert.AreEqual(file.ApplicationInfo.EncryptionInfo.FileDigest, 
                "S+ZXJesMxhj1Opq2AWdJEZlmMJrElayFctjtlx3F/nE=");

            await file.ExtractContentFileAsync("extracted-created-content.intunewin");
            
            Assert.True(FilesAreEqual(
                new FileInfo("extracted-created-content.intunewin"), 
                new FileInfo(@"sample.zip")));
        }
        
        public static bool FilesAreEqual(FileInfo left, FileInfo right) =>
            left.Length == right.Length &&
            (left.Length == 0 || File.ReadAllBytes(left.FullName).SequenceEqual(File.ReadAllBytes(right.FullName)));
    }
}