using System.Text;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoundryLocalChatDemo.Wpf;

/// <summary>
/// Foundry Local の初期化とチャット補完をまとめたクラス(フェーズ1のコンソール版の処理を流用)
/// </summary>
public sealed class ChatService : IDisposable
{
    // 使用するモデルのエイリアス(候補は ModelList.md を参照)
    private const string ModelAlias = "qwen2.5-7b";

    private IModel? _model;

    /// <summary>
    /// SDK 初期化 → 実行プロバイダー登録 → モデルのダウンロード(未キャッシュ時のみ)→ ロードを行う。
    /// 進捗は <paramref name="status"/> に文字列で通知する。
    /// </summary>
    public async Task InitializeAsync(IProgress<string> status)
    {
        status.Report("Foundry Local を初期化しています...");
        await FoundryLocalManager.CreateAsync(
            new Configuration { AppName = "FoundryLocalChatDemo", LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Warning },
            NullLogger.Instance);
        var manager = FoundryLocalManager.Instance;

        // 失敗しても CPU で動作するので結果は確認しない
        await manager.DownloadAndRegisterEpsAsync(
            (epName, percent) => status.Report($"実行プロバイダーを準備しています(初回のみ時間がかかります): {epName} {percent:F1}%"));

        var catalog = await manager.GetCatalogAsync();
        var model = await catalog.GetModelAsync(ModelAlias)
            ?? throw new InvalidOperationException($"モデル '{ModelAlias}' がカタログに見つかりません。");

        if (!await model.IsCachedAsync())
        {
            await model.DownloadAsync(
                progress => status.Report($"モデルをダウンロードしています(初回のみ。数分かかることがあります): {progress:F1}%"));
        }

        status.Report($"モデルをロードしています({model.Id})...");
        await model.LoadAsync();
        _model = model;
        status.Report($"準備完了({model.Id})");
    }

    /// <summary>
    /// 1 回のプロンプトに対する応答を返す。会話履歴は保持しない。
    /// </summary>
    public async Task<string> AskAsync(string prompt)
    {
        var model = _model ?? throw new InvalidOperationException("モデルが初期化されていません。");

        // ChatSession は履歴を蓄積するため、履歴を持たないよう毎回作り直す
        using var session = new ChatSession(model);
        using var request = new Request()
            .AddItem(MessageItem.System("You are a helpful assistant."), false)
            .AddItem(MessageItem.User(prompt), false);
        using var response = await session.ProcessRequestAsync(request, CancellationToken.None);

        return ExtractText(response);
    }

    public void Dispose()
    {
        // プロセス終了時に呼ばれる想定。モデルの解放も含めて SDK 側で後始末される
        if (FoundryLocalManager.IsInitialized)
        {
            FoundryLocalManager.Instance.Dispose();
        }
    }

    // レスポンスに含まれるメッセージのテキスト部分を連結して返す
    private static string ExtractText(Response response)
    {
        var sb = new StringBuilder();
        foreach (var item in response)
        {
            if (item is MessageItem message)
            {
                foreach (var part in message.Parts.OfType<TextItem>())
                {
                    sb.Append(part.Text);
                }
            }
            else if (item is TextItem text)
            {
                sb.Append(text.Text);
            }
        }
        return sb.ToString();
    }
}
