namespace IctBaden.Stonehenge.Extension
{
    public interface IExpandedProvider
    {
        bool GetExpanded(string id);
        void SetExpanded(string id, bool expanded);
    }
}