namespace IctBaden.Stonehenge.TreeView.ViewModels
{
    public interface IExpandedProvider
    {
        bool GetExpanded(string id);
        void SetExpanded(string id, bool expanded);
    }
}