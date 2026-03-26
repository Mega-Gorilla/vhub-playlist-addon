# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

vhub-playlist-addon は YamaPlayer 用の独立 addon package。外部プレイリストサービス (playlist.vrc-hub.com) からプレイリストを読み込み、YamaPlayer の Queue にトラックを自動追加する。

- **パッケージ名**: `com.vhub.playlist-loader`
- **本体依存**: `net.kwxxw.yama-stream` (YamaPlayer) >= 2.0.0
- **本体変更**: なし（本家 YamaPlayer の API のみ使用）
- **サーバー**: https://github.com/kisaragi-official/vhub-playlist
- **Unity version**: 2022.3
- **Language**: C# / UdonSharp

## Architecture

### URL Pool 方式

VRChat/Udon はランタイムで `string → VRCUrl` 変換ができないため、ビルド時に大量の VRCUrl を固定スロットとして焼き込み、サーバーが HTTP 302 リダイレクトでスロット → 実際の動画 URL のマッピングを管理する。

詳細: `docs/design/url-pool-playlist-loader.md`

### ファイル構成

```
Runtime/
  PlaylistLoader.cs       — メインロジック (JSON 取得・パース・Queue 追加・自動再生)
  PlaylistLoaderUI.cs     — UI ハンドラ (onEndEdit 送信、ShowNotification)
Editor/
  PlaylistLoaderEditor.cs — カスタム Inspector (Pool 生成、サーバー検証)
  PlaylistLoaderInstaller.cs — installer (シーン上の YamaPlayer に自動組み込み)
Prefabs/
  PlaylistLoader.prefab       — モジュール本体
  PlaylistLoaderInput.prefab  — URL 入力欄
```

### 本体との連携

- `YamaPlayerModule` を継承（Controller への参照は installer が自動設定）
- `UIController.ShowMessage()` でモーダル通知
- `UIController.GetProgramVariable("_urlInputField")` は使わない（専用入力欄を使用）
- `QueueList.AddTrack()` × N ループで Queue 追加（AddTracks は使わない）

## Key Constraints

- 本家 YamaPlayer の package source asset を直接改変しない
- 本家に追加されていない API (AddTracks 等) に依存しない
- UdonSharp の制約: generics 不可、async/await 不可、配列ベースのデータ構造
- Pool Base URL (`https://playlist.vrc-hub.com`) と Pool Size (`100000`) はサーバー固定

## Design Docs

- `docs/design/url-pool-playlist-loader.md` — 全体設計
- `docs/design/url-pool-unity.md` — Unity 側設計
- `docs/design/url-pool-server.md` — サーバー側設計
- `docs/analysis/why-no-runtime-json-playlist.md` — 背景・動機

## Related Repositories

- [KawaPlayer](https://github.com/Mega-Gorilla/KawaPlayer) (archived) — 開発用フォーク。設計議論・Issue 履歴
- [vhub-playlist](https://github.com/kisaragi-official/vhub-playlist) — サーバー側実装
- [vhub-world-search](https://github.com/kisaragi-official/vhub-world-search) — 姉妹プロジェクト（同構成）
