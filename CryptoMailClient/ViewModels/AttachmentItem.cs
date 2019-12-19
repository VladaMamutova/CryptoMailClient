using System.IO;
using MimeKit;

namespace CryptoMailClient.ViewModels
{
    public class AttachmentItem
    {
        public string FileName { get; }
        public string Extension { get; }
        public MimeEntity Content { get; }

        public AttachmentItem(string fileName, MimeEntity content)
        {
            Extension = Path.GetExtension(fileName);
            FileName = fileName;
            Content = content;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}