using ConcreteEngine.Core.Common.Text;
using ConcreteEngine.Editor.Core;
using static ConcreteEngine.Editor.UI.Core.MenuItem;

namespace ConcreteEngine.Editor.UI.Core;

internal sealed class MenuItem(string name, SubItem[] subMenus)
{
    public readonly string Name = name;
    public readonly SubItem[] SubMenus = subMenus;
    public bool Enabled = true;
    public bool Visible = true;
    
    public sealed class SubItem(string name, string? shortcut, Action<StateManager> onClick)
    {
        public readonly string Name = name;
        public readonly String8Utf8 Shortcut = string.IsNullOrEmpty(shortcut) ? default(String8Utf8) : shortcut;
        public readonly Action<StateManager> OnClick = onClick;
        public bool Enabled = true;
        public bool Visible = true;
    }
}
