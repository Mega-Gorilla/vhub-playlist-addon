using UnityEditor;
using UnityEngine;
using Yamadev.YamaStream;
using Yamadev.YamaStream.Modules.PlaylistLoader;

namespace Vhub.PlaylistLoader.Editor
{
  public static class PlaylistLoaderInstaller
  {
    private const string MenuPath = "Tools/VHub Playlist Loader/Install";
    private const string PlaylistLoaderInputPath = "ScreenUI/Canvas/User/Main/LeftSide/Container";

    [MenuItem(MenuPath)]
    public static void Install()
    {
      // 1. シーン内の YamaPlayer を検出
      var yamaPlayer = Object.FindObjectOfType<YamaPlayer>();
      if (yamaPlayer == null)
      {
        EditorUtility.DisplayDialog("Install Error",
          "シーン内に YamaPlayer が見つかりません。\n\nYamaPlayer を先にシーンに配置してください。", "OK");
        return;
      }

      // 2. 二重導入チェック
      var existingLoader = yamaPlayer.GetComponentInChildren<Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader>();
      if (existingLoader != null)
      {
        EditorUtility.DisplayDialog("Install Warning",
          "PlaylistLoader は既にインストール済みです。\n\n再インストールする場合は、先に既存の PlaylistLoader を削除してください。", "OK");
        return;
      }

      // 3. Controller を取得
      var controller = yamaPlayer.GetComponentInChildren<Controller>();
      if (controller == null)
      {
        EditorUtility.DisplayDialog("Install Error",
          "YamaPlayer の Controller が見つかりません。", "OK");
        return;
      }

      // 4. PlaylistLoader GameObject を作成
      var loaderGo = new GameObject("PlaylistLoader");
      loaderGo.transform.SetParent(yamaPlayer.transform);
      Undo.RegisterCreatedObjectUndo(loaderGo, "Install PlaylistLoader");

      // 5. PlaylistLoader コンポーネントを追加
      var loader = loaderGo.AddComponent<Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader>();

      // 6. PlaylistLoaderUI コンポーネントを追加
      var loaderUI = loaderGo.AddComponent<PlaylistLoaderUI>();

      // 7. Controller 参照を設定 (リフレクションで _controller を設定)
      var controllerField = typeof(YamaPlayerModule).GetField("_controller",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (controllerField != null)
      {
        controllerField.SetValue(loader, controller);
      }

      // 8. _ui 参照を設定
      var uiField = typeof(Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader).GetField("_ui",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (uiField != null)
      {
        uiField.SetValue(loader, loaderUI);
      }

      // 9. _loader 参照を設定
      var loaderRefField = typeof(PlaylistLoaderUI).GetField("_loader",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (loaderRefField != null)
      {
        loaderRefField.SetValue(loaderUI, loader);
      }

      // 10. PlaylistLoaderInput を UI 階層に配置
      var container = yamaPlayer.transform.Find(PlaylistLoaderInputPath);
      if (container != null)
      {
        // TODO: PlaylistLoaderInput prefab をインスタンス化して配置
        // 現在は prefab が未作成のため、手動配置が必要
        Debug.Log("[PlaylistLoader Installer] PlaylistLoaderInput の配置は手動で行ってください。");
        Debug.Log($"[PlaylistLoader Installer] 配置先: {PlaylistLoaderInputPath}");
      }
      else
      {
        Debug.LogWarning("[PlaylistLoader Installer] UI 階層 (LeftSide/Container) が見つかりません。PlaylistLoaderInput は手動で配置してください。");
      }

      EditorUtility.SetDirty(loaderGo);
      EditorUtility.SetDirty(loader);
      EditorUtility.SetDirty(loaderUI);

      Debug.Log("[PlaylistLoader Installer] インストール完了");
      EditorUtility.DisplayDialog("Install Complete",
        "PlaylistLoader をインストールしました。\n\n次の手順:\n1. Inspector で Pool ID を確認\n2. Generate Pool を実行\n3. PlaylistLoaderInput を手動配置", "OK");
    }
  }
}
