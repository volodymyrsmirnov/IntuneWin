using System;
using System.Xml.Serialization;

namespace IntuneWin.Data
{
    /// <summary>
    /// Application info model.
    /// </summary>
    [XmlRoot("ApplicationInfo")]
    [Serializable]
    public class ApplicationInfo
    {
        /// <summary>
        /// The version of the tool. Looks like 1.4.0.0 is the latest one for now.
        /// </summary>
        [XmlAttribute]
        public string ToolVersion { get; set; } = "1.4.0.0";

        /// <summary>
        /// Application name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Application description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Unencrypted content size.
        /// </summary>
        public long UnencryptedContentSize { get; set; }

        /// <summary>
        /// Content file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Setup file name within the content file.
        /// </summary>
        public string SetupFile { get; set; }

        /// <summary>
        /// Encryption information.
        /// </summary>
        public FileEncryptionInfo EncryptionInfo { get; set; }
    }
}