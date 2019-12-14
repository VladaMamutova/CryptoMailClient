namespace CryptoMailClient.ViewModels
{
    public class FolderItem
    {
        public string Name { get; set; }
        public int Count { get; set; }

        public FolderItem(string name, int count)
        {
            Name = name;
            Count = count;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}