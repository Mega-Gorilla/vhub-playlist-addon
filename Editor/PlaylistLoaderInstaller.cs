using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
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
      // 1. シーン内の YamaPlayer を全件検出
      var allPlayers = Object.FindObjectsOfType<YamaPlayer>();
      if (allPlayers.Length == 0)
      {
        EditorUtility.DisplayDialog("Install Error",
          "シーン内に YamaPlayer が見つかりません。\n\nYamaPlayer を先にシーンに配置してください。", "OK");
        return;
      }

      // 2. 未導入の YamaPlayer だけを候補にする
      var candidates = new System.Collections.Generic.List<YamaPlayer>();
      foreach (var p in allPlayers)
      {
        if (p.GetComponentInChildren<Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader>() == null)
          candidates.Add(p);
      }

      if (candidates.Count == 0)
      {
        EditorUtility.DisplayDialog("Install Warning",
          "全ての YamaPlayer に PlaylistLoader が導入済みです。\n\n再インストールする場合は、先に既存の PlaylistLoader を削除してください。", "OK");
        return;
      }

      // 3. 対象の YamaPlayer を決定
      YamaPlayer yamaPlayer = ResolveTargetPlayer(candidates);
      if (yamaPlayer == null) return;

      // 4. Controller を取得
      var controller = yamaPlayer.GetComponentInChildren<Controller>();
      if (controller == null)
      {
        EditorUtility.DisplayDialog("Install Error",
          "YamaPlayer の Controller が見つかりません。", "OK");
        return;
      }

      Undo.SetCurrentGroupName("Install PlaylistLoader");

      // 5. PlaylistLoader GameObject を作成
      var loaderGo = new GameObject("PlaylistLoader");
      loaderGo.transform.SetParent(yamaPlayer.transform);
      Undo.RegisterCreatedObjectUndo(loaderGo, "Create PlaylistLoader");

      // 6. PlaylistLoader コンポーネントを追加
      var loader = loaderGo.AddComponent<Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader>();

      // 7. PlaylistLoaderUI コンポーネントを追加
      var loaderUI = loaderGo.AddComponent<PlaylistLoaderUI>();

      // 8. Controller 参照を設定 (未設定でもランタイムで自動検出される)
      SetField(loader, typeof(Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader), "_controller", controller);

      // 9. _ui 参照を設定
      SetField(loader, typeof(Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader), "_ui", loaderUI);

      // 10. _loader 参照を設定
      SetField(loaderUI, typeof(PlaylistLoaderUI), "_loader", loader);

      // 11. PlaylistLoaderInput prefab を UI 階層に配置
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

      // 12. _playlistUrlInput 参照を接続
      var urlInput = inputInstance.GetComponent<VRC.SDK3.Components.VRCUrlInputField>();
      if (urlInput != null)
      {
        SetField(loaderUI, typeof(PlaylistLoaderUI), "_playlistUrlInput", urlInput);
      }
      else
      {
        Debug.LogWarning("[PlaylistLoader Installer] PlaylistLoaderInput に VRCUrlInputField が見つかりません。");
      }

      // 13. _statusText 参照を接続 (Prefab 内の Text コンポーネントを検索)
      var statusText = inputInstance.GetComponentInChildren<Text>(true);
      if (statusText != null)
      {
        SetField(loaderUI, typeof(PlaylistLoaderUI), "_statusText", statusText);
      }
      else
      {
        Debug.LogWarning("[PlaylistLoader Installer] PlaylistLoaderInput に Text コンポーネントが見つかりません。通知は UIController.ShowMessage のみで動作します。");
      }

      EditorUtility.SetDirty(loaderGo);
      EditorUtility.SetDirty(loader);
      EditorUtility.SetDirty(loaderUI);

      Debug.Log("[PlaylistLoader Installer] インストール完了");
      EditorUtility.DisplayDialog("Install Complete",
        "PlaylistLoader をインストールしました。\n\n次の手順:\n1. Inspector で Pool ID を確認\n2. Generate Pool を実行", "OK");
    }

    private static YamaPlayer ResolveTargetPlayer(System.Collections.Generic.List<YamaPlayer> candidates)
    {
      if (candidates.Count == 1) return candidates[0];

      var selected = Selection.activeGameObject;
      if (selected != null)
      {
        foreach (var candidate in candidates)
        {
          if (selected == candidate.gameObject || selected.transform.IsChildOf(candidate.transform))
            return candidate;
        }
      }

      var names = new string[candidates.Count];
      for (int i = 0; i < candidates.Count; i++)
        names[i] = candidates[i].gameObject.name;

      EditorUtility.DisplayDialog("YamaPlayer を選択",
        $"シーン内に {candidates.Count} 台の未導入 YamaPlayer があります:\n\n" +
        string.Join("\n", names) +
        "\n\nHierarchy で対象の YamaPlayer を選択してから再実行してください。", "OK");
      return null;
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
