using ImGuiNET;

namespace Core.DebugTools.Utils;

internal struct ImGuiPopupTracker
{
    private bool _wasOpen;
    private bool _isOpen;

    public void Update(ReadOnlySpan<char> popupName)
    {
        _isOpen = ImGui.IsPopupOpen(popupName);
    }

    public bool JustClosed()
    {
        bool closed = !_isOpen && _wasOpen;
        _wasOpen = _isOpen;
        return closed;
    }
}