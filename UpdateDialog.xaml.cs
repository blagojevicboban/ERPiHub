using System;
using System.Threading.Tasks;
using System.Windows;
using Velopack;

namespace ERPiHub;

public partial class UpdateDialog : Window
{
    private readonly UpdateInfo _updateInfo;
    private readonly UpdateManager _updateManager;

    public UpdateDialog(UpdateInfo updateInfo, UpdateManager updateManager)
    {
        InitializeComponent();
        _updateInfo = updateInfo;
        _updateManager = updateManager;

        MessageText.Text = $"Dostupna je nova verzija ERP Hub-a (v{_updateInfo.TargetFullRelease.Version}). Da li želite da je preuzmete i instalirate sada?";
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            MessageText.Text = "Preuzimanje ažuriranja ERP Hub-a. Molimo sačekajte...";

            await _updateManager.DownloadUpdatesAsync(_updateInfo, (progress) =>
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateProgress.Value = progress;
                    ProgressText.Text = $"Preuzimanje: {progress}%";
                });
            });

            ProgressText.Text = "Ažuriranje preuzeto! ERP Hub se ponovo pokreće...";
            await Task.Delay(1000);

            _updateManager.ApplyUpdatesAndRestart(_updateInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Došlo je do greške pri ažuriranju ERP Hub-a:\n{ex.Message}",
                "Greška pri ažuriranju",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            DialogResult = false;
            Close();
        }
    }
}
