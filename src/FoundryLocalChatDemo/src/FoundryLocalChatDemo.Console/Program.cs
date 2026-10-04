using System.Text;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.Logging.Abstractions;

// フェーズ1: Foundry Local SDK の疎通確認用コンソールアプリ
//   dotnet run                 … 既定のプロンプトで 1 回チャット補完を実行
//   dotnet run -- "質問文"     … 任意のプロンプトで実行
//   dotnet run -- --list       … カタログのモデル一覧を表示(モデル選定用)

// 使用するモデルのエイリアス(小さく軽いモデルを選定)
const string ModelAlias = "qwen2.5-7b";

Console.OutputEncoding = Encoding.UTF8;

var listOnly = args.Contains("--list");
var prompt = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "空が青いのはなぜですか?簡潔に答えてください。";

// 1. SDK 初期化(シングルトン)
Console.WriteLine("Foundry Local を初期化しています...");
await FoundryLocalManager.CreateAsync(
    new Configuration { AppName = "FoundryLocalChatDemo", LogLevel = Microsoft.AI.Foundry.Local.LogLevel.Warning },
    NullLogger.Instance);
using var manager = FoundryLocalManager.Instance;

// 2. ハードウェアアクセラレーション用の実行プロバイダー(EP)を登録
//    初回はダウンロードが走る。失敗しても CPU で動作するので続行する
Console.WriteLine("実行プロバイダーを準備しています(初回はダウンロードのため時間がかかります)...");
var epResult = await manager.DownloadAndRegisterEpsAsync(
    (epName, percent) => Console.Write($"\r  {epName}: {percent,6:F1}%   "));
Console.WriteLine();
Console.WriteLine(epResult.Success
    ? $"  登録済み EP: {string.Join(", ", epResult.RegisteredEps)}"
    : $"  EP の登録に失敗しました({epResult.Status})。CPU で続行します。");

var catalog = await manager.GetCatalogAsync();

if (listOnly)
{
    // モデル選定用にチャット系モデルの一覧をサイズ順で表示する
    var models = await catalog.ListModelsAsync();
    var rows = models
        .SelectMany(m => m.Variants)
        .Select(v => v.Info)
        .Where(i => i.Task == "chat-completion")
        .OrderBy(i => i.FileSizeMb ?? int.MaxValue);
    foreach (var info in rows)
    {
        Console.WriteLine($"{info.Alias,-28} {info.Id,-50} {info.FileSizeMb,7} MB  {info.Runtime?.DeviceType,-4} {(info.Cached ? "cached" : "")}");
    }
    return;
}

// 3. モデル取得 → ダウンロード(未キャッシュ時のみ)→ ロード
var model = await catalog.GetModelAsync(ModelAlias)
    ?? throw new InvalidOperationException($"モデル '{ModelAlias}' がカタログに見つかりません。--list で一覧を確認してください。");
Console.WriteLine($"モデル: {model.Id}");

if (!await model.IsCachedAsync())
{
    Console.WriteLine("モデルをダウンロードしています(初回のみ。数分かかることがあります)...");
    await model.DownloadAsync(progress => Console.Write($"\r  {progress,6:F1}%   "));
    Console.WriteLine();
}

Console.WriteLine("モデルをロードしています...");
await model.LoadAsync();

// 4. チャット補完(Session API・非ストリーミング)
try
{
    Console.WriteLine();
    Console.WriteLine($"> {prompt}");

    using var session = new ChatSession(model);
    using var request = new Request()
        .AddItem(MessageItem.System("You are a helpful assistant."), false)
        .AddItem(MessageItem.User(prompt), false);
    using var response = await session.ProcessRequestAsync(request, CancellationToken.None);

    Console.WriteLine(ExtractText(response));
    Console.WriteLine();
    var usage = response.GetUsage();
    Console.WriteLine($"(終了理由: {response.FinishReason} / トークン: 入力 {usage.PromptTokens}, 出力 {usage.CompletionTokens})");
}
finally
{
    await model.UnloadAsync();
}

// レスポンスに含まれるメッセージのテキスト部分を連結して返す
static string ExtractText(Response response)
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
