using ServidorControlRemoto.Services;

namespace WinFormsApp1;

public partial class Form1 : Form
{
    private NetworkService _networkService;
    public Form1()
    {
        InitializeComponent();
        _networkService = new NetworkService();
        Task.Run(() => _networkService.StartAsync());
        
    }
}