using System.Windows.Forms;

namespace DirectoryComparer.Components
{
    // https://www.codeproject.com/Articles/5255769/Csharp-Select-FolderDialog-for-NET-Core-3-0
    public interface IFolderDialog
    {
        string InitialFolder { get; set; }
        string DefaultFolder { get; set; }
        string Folder { get; set; }
        DialogResult ShowDialog();
        DialogResult ShowDialog(IWin32Window owner);
        DialogResult ShowVistaDialog(IWin32Window owner);
        DialogResult ShowLegacyDialog(IWin32Window owner);
        void Dispose();
    }
}