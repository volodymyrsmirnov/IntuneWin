using System;
using System.Xml.Serialization;

namespace IntuneWin.Data
{
    /// <summary>
    /// File encryption info.
    /// </summary>
    [XmlRoot("EncryptionInfo")]
    [Serializable]
    public class FileEncryptionInfo
    {
        /// <summary>
        /// Profile identifier.
        /// </summary>
        public string ProfileIdentifier { get; set; } = "ProfileVersion1";

        /// <summary>
        /// AES encryption key.
        /// </summary>
        public string EncryptionKey { get; set; }

        /// <summary>
        /// AES IV.
        /// </summary>
        public string InitializationVector { get; set; }

        /// <summary>
        /// HMAC checksum.
        /// </summary>
        public string Mac { get; set; }
        
        /// <summary>
        /// HMAC key.
        /// </summary>
        public string MacKey { get; set; }
        
        /// <summary>
        /// Content file checksum.
        /// </summary>
        public string FileDigest { get; set; }

        /// <summary>
        /// Content file checksum algorithm.
        /// </summary>
        public string FileDigestAlgorithm { get; set; } = "SHA256";
    }
}