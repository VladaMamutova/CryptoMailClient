using System.IO;

namespace CryptoMailClient.ViewModels
{
    public class FolderItem
    {
        public string FullName { get; }
        public string DisplayName { get; }
        public int Count { get; set; }

        public FolderItem(string fullName, string displayName, int count)
        {
            FullName = fullName;
            DisplayName = displayName;
            Count = count;
        }

        public FolderItem(string path, int count)
        {
            FullName = path;
            DisplayName = Path.GetDirectoryName(path);
            Count = count;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}