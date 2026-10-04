using System.Windows;

namespace FoundryLocalChatDemo.Wpf;

/// <summary>
/// 入力欄・送信ボタン・応答表示のみの最小チャット画面
/// </summary>
public partial class MainWindow : Window
{
    private readonly ChatService _chatService = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    // 起動時に 1 回だけモデルを準備する(以降の送信ではロード待ちが発生しない)
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // IProgress は UI スレッドで生成するので、コールバックは UI スレッドで実行される
        var status = new Progress<string>(message => StatusText.Text = message);
        try
        {
            // SDK の処理が UI スレッドを塞がないようバックグラウンドで実行する
            await Task.Run(() => _chatService.InitializeAsync(status));
            SendButton.IsEnabled = true;
            PromptText.Focus();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"初期化に失敗しました: {ex.Message}";
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var prompt = PromptText.Text.Trim();
        if (prompt.Length == 0)
        {
            return;
        }

        // 推論中は二重送信を防ぐため入力を無効化する
        SendButton.IsEnabled = false;
        PromptText.IsEnabled = false;
        var statusBefore = StatusText.Text;
        StatusText.Text = "応答を生成しています...";
        ResponseText.Text = $"> {prompt}\n\n";
        try
        {
            var answer = await Task.Run(() => _chatService.AskAsync(prompt));
            ResponseText.Text += answer;
            PromptText.Clear();
            StatusText.Text = statusBefore;
        }
        catch (Exception ex)
        {
            StatusText.Text = $"応答の生成に失敗しました: {ex.Message}";
        }
        finally
        {
            SendButton.IsEnabled = true;
            PromptText.IsEnabled = true;
            PromptText.Focus();
        }
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        _chatService.Dispose();
    }
}
