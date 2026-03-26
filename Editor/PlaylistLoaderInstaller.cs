using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using VRC.Udon;
using Yamadev.YamaStream;
using Yamadev.YamaStream.Modules.PlaylistLoader;

namespace Vhub.PlaylistLoader.Editor
{
  public static class PlaylistLoaderInstaller
  {
    private const string MenuPath = "Tools/VHub Playlist Loader/Install";
    private const string PlaylistLoaderInputPath = "ScreenUI/Canvas/User/Main/LeftSide/Container";
    private const string InputPrefabPath = "Packages/com.vhub.playlist-loader/Prefabs/PlaylistLoaderInput.prefab";

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

      Undo.SetCurrentGroupName("Install PlaylistLoader");

      // 4. PlaylistLoader GameObject を作成
      var loaderGo = new GameObject("PlaylistLoader");
      loaderGo.transform.SetParent(yamaPlayer.transform);
      Undo.RegisterCreatedObjectUndo(loaderGo, "Create PlaylistLoader");

      // 5. PlaylistLoader コンポーネントを追加
      var loader = loaderGo.AddComponent<Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader>();

      // 6. PlaylistLoaderUI コンポーネントを追加
      var loaderUI = loaderGo.AddComponent<PlaylistLoaderUI>();

      // 7. Controller の UdonBehaviour 参照を設定 (未設定でもランタイムで自動検出される)
      var controllerUdon = controller.GetComponent<UdonBehaviour>();
      if (controllerUdon != null)
      {
        SetField(loader, typeof(Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader), "_controller", controllerUdon);
      }
      else
      {
        Debug.LogWarning("[PlaylistLoader Installer] Controller の UdonBehaviour が見つかりません。ランタイムで自動検出されます。");
      }

      // 8. _ui 参照を設定
      SetField(loader, typeof(Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader), "_ui", loaderUI);

      // 9. _loader 参照を設定
      SetField(loaderUI, typeof(PlaylistLoaderUI), "_loader", loader);

      // 10. PlaylistLoaderInput prefab を UI 階層に配置
      var container = yamaPlayer.transform.Find(PlaylistLoaderInputPath);
      if (container == null)
      {
        Debug.LogWarning("[PlaylistLoader Installer] UI 階層 (LeftSide/Container) が見つかりません。");
        EditorUtility.DisplayDialog("Install Warning",
          "PlaylistLoader のコンポーネントは追加しましたが、\nUI 階層 (LeftSide/Container) が見つかりませんでした。\n\nPlaylistLoaderInput は手動で配置してください。", "OK");
        return;
      }

      var inputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(InputPrefabPath);
      if (inputPrefab == null)
      {
        Debug.LogError($"[PlaylistLoader Installer] PlaylistLoaderInput prefab が見つかりません: {InputPrefabPath}");
        EditorUtility.DisplayDialog("Install Error",
          $"PlaylistLoaderInput prefab が見つかりません。\n\nパス: {InputPrefabPath}\n\nパッケージが正しくインストールされているか確認してください。", "OK");
        return;
      }

      // Prefab をインスタンス化して Container に配置
      var inputInstance = (GameObject)PrefabUtility.InstantiatePrefab(inputPrefab, container);
      inputInstance.name = "PlaylistLoaderInput";
      Undo.RegisterCreatedObjectUndo(inputInstance, "Create PlaylistLoaderInput");

      // 11. _playlistUrlInput 参照を接続
      var urlInput = inputInstance.GetComponent<VRC.SDK3.Components.VRCUrlInputField>();
      if (urlInput != null)
      {
        SetField(loaderUI, typeof(PlaylistLoaderUI), "_playlistUrlInput", urlInput);

        // 12. onEndEdit イベントを PlaylistLoaderUI.OnPlaylistUrlSubmit に接続
        var uiUdon = loaderUI.GetComponent<UdonBehaviour>();
        if (uiUdon != null)
        {
          UnityEventTools.AddStringPersistentListener(
              urlInput.onEndEdit, uiUdon.SendCustomEvent, "OnPlaylistUrlSubmit");
        }
        else
        {
          Debug.LogWarning("[PlaylistLoader Installer] PlaylistLoaderUI の UdonBehaviour が見つかりません。onEndEdit イベントを手動で接続してください。");
        }
      }
      else
      {
        Debug.LogWarning("[PlaylistLoader Installer] PlaylistLoaderInput に VRCUrlInputField が見つかりません。");
      }

      EditorUtility.SetDirty(loaderGo);
      EditorUtility.SetDirty(loader);
      EditorUtility.SetDirty(loaderUI);

      Debug.Log("[PlaylistLoader Installer] インストール完了");
      EditorUtility.DisplayDialog("Install Complete",
        "PlaylistLoader をインストールしました。\n\n次の手順:\n1. Inspector で Pool ID を確認\n2. Generate Pool を実行", "OK");
    }

    private static void SetField(object target, System.Type type, string fieldName, object value)
    {
      var field = type.GetField(fieldName,
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
      if (field != null)
      {
        field.SetValue(target, value);
      }
      else
      {
        Debug.LogWarning($"[PlaylistLoader Installer] Field '{fieldName}' not found on {type.Name}");
      }
    }
  }
}
