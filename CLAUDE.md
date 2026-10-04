# CLAUDE.md

このファイルは、本リポジトリで Claude Code が作業する際のガイドです。
実装を進める前に必ず読み、方針に沿って進めてください。

## プロジェクトの目的

Windows デスクトップアプリとして、**Foundry Local を使ったシンプルなオンデバイス LLM 体験**を作る。
運用コストゼロ(クラウドAPI課金なし、オフラインで完結)が最優先の要件。
機能の豊富さより「まず動くものを最短で」が目的なので、過剰な抽象化や先回りの拡張は避けること。

## 技術選定(決定済み・変更する場合は必ず確認を取ること)

- 言語/ランタイム: **C# / .NET 9 SDK 以降**(.NET 10 が入っていればそちらでも可。プロジェクトファイルの TargetFramework は実装時に環境へ合わせる)
- Foundry Local 連携: **`Microsoft.AI.Foundry.Local`** NuGet パッケージ(2.x 系)を使う
  - 在プロセス(in-process)SDK。HTTP 経由でなくネイティブ API として呼び出せるため実装がシンプル
  - エンドユーザー側に Foundry Local CLI のインストールを要求しない(配布時の障壁が低い)
  - 当初は `Microsoft.AI.Foundry.Local.WinML` を予定していたが、2.x で WinML 版は廃止・本パッケージに統合された(Windows では WinML アクセラレーションが自動で有効)。WinML 版は 1.2.4 が最終のため使わない(2026-10 ユーザー承認済み)
  - チャット呼び出しは Session API(`ChatSession` / `Request` / `MessageItem` / `Response`)を使う。旧 `GetChatClientAsync`(OpenAI 形式クライアント)は非推奨で 2026 年末に削除予定
- UI: 最初はコンソールアプリで疎通確認 → 動作確認後に **WPF** の最小構成(入力欄・送信ボタン・応答表示エリアのみ)に昇格
  - WinUI 3 ではなく WPF を選ぶ理由: セットアップが軽く、今回の目的(体験してみる)に対して学習コストが低いため
- モデル: 具体的なモデルID(例: phi-4-mini-instruct など)は Foundry Local のカタログが流動的なため、このファイルには固定しない
  - 実装時に SDK のカタログ一覧(またはオプションでインストールした `foundry model ls`)から、**小さく・ダウンロードが軽いモデル**を選定すること
  - 選定したモデルIDは実装後に本ファイルの「現在の状態」セクションに追記する

## 前提環境(実装前に確認)

- Windows 11, version 24H2 (build 26100) 以降
- .NET 9.0 SDK 以降がインストール済み
- x64 または Arm64 デバイスで、選定モデルに見合うメモリ・ディスク空き容量があること
- (任意・開発時のみ推奨)Foundry Local CLI をインストールしておくと `foundry model ls` 等でカタログやキャッシュ状況を手元で確認できる。SDK の実行自体には不要

これらが未確認の場合は、実装に入る前にユーザーに確認すること。

## ディレクトリ構成(予定)

```
/
├── CLAUDE.md
├── README.md
└── src/
    └── FoundryLocalChatDemo/
        ├── FoundryLocalChatDemo.slnx          # .NET 10 SDK で作成した新形式のソリューション
        └── src/
            ├── FoundryLocalChatDemo.Console/  # フェーズ1: 疎通確認用コンソールアプリ
            └── FoundryLocalChatDemo.Wpf/      # フェーズ2: 最小限のチャットUI

```

ソリューションとコンソールプロジェクトは作成済み。WPF プロジェクトはフェーズ2で追加する。

## 実装の進め方(フェーズ分割)

### フェーズ1: コンソールで疎通確認
1. `dotnet new console` でプロジェクト作成
2. `Microsoft.AI.Foundry.Local` パッケージを追加(バージョンは NuGet.org で最新を確認してから指定する)
3. SDK を初期化し、1回のプロンプト→応答のチャット補完を実行できることを確認する
4. 初回実行時はモデルのダウンロードが走るため、進捗ログを表示し、ユーザーに「初回は時間がかかる」旨が分かるようにする
5. `dotnet run` で実際に応答が返ることを確認してから次フェーズへ進む

### フェーズ2: 最小限のWPF UI
1. 入力用 TextBox、送信ボタン、応答表示用 TextBlock(またはスクロール可能な領域)のみのシングルウィンドウ
2. フェーズ1で確認したチャット補完呼び出しをそのまま流用
3. 推論中はボタンを非活性にする等、最低限のフリーズ防止(非同期呼び出し)だけ入れる
4. 会話履歴の保持、ストリーミング表示、モデル切り替えUIなどは今回のスコープ外(やりたくなったら相談してから着手)

## コーディング規約

- コメント・ドキュメントは日本語で書く
- 変数名・クラス名などコードの識別子は英語で一般的な命名規則に従う
- 外部ライブラリのバージョンを CLAUDE.md や会話内の古い情報から類推しない。NuGet/公式ドキュメントで必ず現在値を確認する(Foundry Local はまだ preview 段階で変更が速いため)
- 大きな設計変更(UIフレームワークの変更、SDKパッケージの変更など)をする前には、必ずユーザーに確認する

## 既知の留意点

- Foundry Local はまだ preview 段階。API やパッケージ構成が変わる可能性があるため、公式ドキュメント(`learn.microsoft.com/windows/ai/foundry-local/`)と食い違う場合は公式を優先する
- SDK はネイティブバイナリを含むため RuntimeIdentifier の指定が必須(未指定だと NETSDK1047)。コンソールの csproj では実行マシンのアーキテクチャから `win-x64`/`win-arm64` を自動設定している。WPF でも同様にすること
- 実行プロバイダー(EP)は `DownloadAndRegisterEpsAsync` で明示的にダウンロード・登録する。初回は数百MB〜のダウンロードが走る(NVIDIA GPU 環境では TensorRT RTX / CUDA / WebGPU が登録された)
- Copilot+ PC(NPU搭載)でなくても動作するが、GPU/CPUのみの場合はパフォーマンス重視でより軽量なモデルを選ぶこと

## 現在の状態

(実装が進むごとにここを更新すること)

- [x] フェーズ1(コンソール疎通確認): 完了(2026-10-05、`dotnet run` で応答を確認)
- [ ] フェーズ2(WPF UI): 未着手
- SDK: `Microsoft.AI.Foundry.Local` 2.1.0 / TargetFramework `net9.0`
- 選定モデル: `qwen2.5-7b`(エイリアス。GPU 版で約 4.8GB。実行環境に応じて SDK がバリアントを自動選択し、開発機では `qwen2.5-7b-instruct-trtrtx-gpu:2` が選ばれた)
  - 疎通確認時は `qwen2.5-0.5b` を使っていたが、日本語の品質を上げるため 2026-10-05 に切り替えた。候補の比較は ModelList.md を参照
  - モデル候補の一覧は `dotnet run --project src/FoundryLocalChatDemo/src/FoundryLocalChatDemo.Console -- --list` で確認できる
