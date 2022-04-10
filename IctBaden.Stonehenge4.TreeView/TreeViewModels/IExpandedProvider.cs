namespace IctBaden.Stonehenge.Extension.TreeViewModels
{
    public interface IExpandedProvider
    {
        bool GetExpanded(string id);
        void SetExpanded(string id, bool expanded);
    }
}