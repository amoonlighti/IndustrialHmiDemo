using System.Windows;

namespace IndustrialHmiDemo;

/// <summary>
/// 主窗口只负责加载界面。
/// 具体业务逻辑放在 MainViewModel 中，这样界面和业务更容易分开维护。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
