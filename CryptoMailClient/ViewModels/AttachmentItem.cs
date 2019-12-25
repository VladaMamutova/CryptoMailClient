using System.IO;
using MimeKit;

namespace CryptoMailClient.ViewModels
{
    public class AttachmentItem
    {
        public string FullName { get; }
        public string FileName { get; }
        public string Extension { get; }
        public MimeEntity Content { get; }

        public AttachmentItem(string fileName, MimeEntity content)
        {
            Extension = Path.GetExtension(fileName);
            FullName = FileName = fileName;
            Content = content;
        }

        public AttachmentItem(string fullName)
        {
            FullName = fullName;
            Extension = Path.GetExtension(fullName);
            FileName = Path.GetFileName(fullName);
            Content = null;
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}