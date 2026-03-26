# vhub-playlist-addon

YamaPlayer 用プレイリスト読み込みアドオン。外部プレイリストサービス ([playlist.vrc-hub.com](https://playlist.vrc-hub.com)) からプレイリストを読み込み、YamaPlayer の Queue にトラックを自動追加します。

## 要件

- **YamaPlayer** >= 2.0.0
- **VRChat Worlds SDK** >= 3.8.1
- **Unity** 2022.3

## 導入手順

1. [YamaPlayer](https://github.com/koorimizuw/YamaPlayer) を導入（VPM 推奨）
2. 本パッケージを Unity プロジェクトにインポート
3. Unity メニューから `Tools > VHub Playlist Loader > Install` を実行
4. シーン上の YamaPlayer を選択し、installer が自動セットアップ
5. Inspector で Pool ID を設定し、`Generate Pool` を実行

## 使い方

1. [playlist.vrc-hub.com](https://playlist.vrc-hub.com) でプレイリストを作成
2. プレイリストページの VRChat URL をコピー
3. VRChat ワールド内の PlaylistLoaderInput に URL を貼り付け
4. Enter を押すとプレイリストが Queue に追加され、自動再生

## 関連

- [vhub-playlist](https://github.com/kisaragi-official/vhub-playlist) — サーバー側実装
- [KawaPlayer](https://github.com/Mega-Gorilla/KawaPlayer) (archived) — 開発用フォーク・設計議論
