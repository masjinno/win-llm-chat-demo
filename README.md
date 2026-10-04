# win-llm-chat-demo

[Foundry Local](https://learn.microsoft.com/windows/ai/foundry-local/) を使って、Windows 上で **オンデバイス LLM とチャットする** ための最小構成のデスクトップアプリです。

- **運用コストゼロ**: クラウド API を使わないので課金なし。モデルのダウンロード後はオフラインで動作します
- **追加インストール不要**: `Microsoft.AI.Foundry.Local` NuGet パッケージ(在プロセス SDK。Windows では WinML アクセラレーションが自動で有効)を使うため、エンドユーザーが Foundry Local CLI を入れる必要はありません
- **シンプル**: 機能の豊富さより「まず動くこと」を優先しています

> [!NOTE]
> 現在は開発初期段階です。進捗は下の「開発状況」を参照してください。

## 開発状況

| フェーズ | 内容 | 状態 |
| --- | --- | --- |
| 1 | コンソールアプリで SDK 初期化 → 1 回のプロンプト/応答を確認 | 完了 |
| 2 | WPF の最小チャット UI(入力欄・送信ボタン・応答表示のみ) | 完了 |

使用モデル: `qwen2.5-7b`(約 4.8〜6.3GB。GPU / CPU に応じたバリアントを SDK が自動選択します)。候補の比較は [ModelList.md](ModelList.md) を参照

## 動作環境

- Windows 11 version 24H2(ビルド 26100)以降
- [.NET 9 SDK](https://dotnet.microsoft.com/download) 以降
- x64 または Arm64 の PC
- モデルに見合うメモリとディスクの空き容量

NPU 搭載の Copilot+ PC でなくても動作します(GPU / CPU で推論します)。

## 使い方

リポジトリのルートで実行します。

```powershell
# ソリューション全体をビルド
dotnet build src/FoundryLocalChatDemo/FoundryLocalChatDemo.slnx

# フェーズ1: コンソールで疎通確認(既定の質問で 1 回応答を返す)
dotnet run --project src/FoundryLocalChatDemo/src/FoundryLocalChatDemo.Console

# 任意の質問を渡す
dotnet run --project src/FoundryLocalChatDemo/src/FoundryLocalChatDemo.Console -- "C# の async/await を一言で説明して"

# カタログのモデル一覧(サイズ順)を表示する
dotnet run --project src/FoundryLocalChatDemo/src/FoundryLocalChatDemo.Console -- --list

# フェーズ2: WPF アプリを起動(起動時にモデルを読み込み、「準備完了」と表示されたら質問を送れる)
dotnet run --project src/FoundryLocalChatDemo/src/FoundryLocalChatDemo.Wpf
```

> [!IMPORTANT]
> 初回実行時は実行プロバイダー(GPU 用ランタイム等)とモデルのダウンロードが行われるため、回線状況によっては数分以上かかります。2 回目以降はキャッシュ済みのモデルを使います。

## ディレクトリ構成

```
/
└── src/
    └── FoundryLocalChatDemo/
        ├── FoundryLocalChatDemo.slnx
        └── src/
            ├── FoundryLocalChatDemo.Console/  # フェーズ1: 疎通確認用コンソールアプリ
            └── FoundryLocalChatDemo.Wpf/      # フェーズ2: 最小限のチャット UI
```

## スコープ外

今回は以下を扱いません。

- 会話履歴の保持
- ストリーミング表示
- モデル切り替え UI
- Windows 以外の OS への対応

## 開発者向けメモ

- Foundry Local はプレビュー段階のため、API やパッケージ構成が変わる可能性があります。食い違いがあれば[公式ドキュメント](https://learn.microsoft.com/windows/ai/foundry-local/)を優先してください
- [Foundry Local CLI](https://learn.microsoft.com/windows/ai/foundry-local/) を任意で入れておくと、`foundry model ls` でカタログやキャッシュの状態を確認できて便利です(アプリの実行には不要)
- 開発方針の詳細は [CLAUDE.md](CLAUDE.md) を参照してください

## ライセンス

[Apache License 2.0](LICENSE)
