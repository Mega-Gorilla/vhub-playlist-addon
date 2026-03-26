using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
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

      Undo.SetCurrentGroupName("Install PlaylistLoader");

      // 4. PlaylistLoader GameObject を作成
      var loaderGo = new GameObject("PlaylistLoader");
      loaderGo.transform.SetParent(yamaPlayer.transform);
      Undo.RegisterCreatedObjectUndo(loaderGo, "Create PlaylistLoader");

      // 5. PlaylistLoader コンポーネントを追加
      var loader = loaderGo.AddComponent<Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader>();

      // 6. PlaylistLoaderUI コンポーネントを追加
      var loaderUI = loaderGo.AddComponent<PlaylistLoaderUI>();

      // 7. Controller 参照を設定
      SetField(loader, typeof(YamaPlayerModule), "_controller", controller);

      // 8. _ui 参照を設定
      SetField(loader, typeof(Yamadev.YamaStream.Modules.PlaylistLoader.PlaylistLoader), "_ui", loaderUI);

      // 9. _loader 参照を設定
      SetField(loaderUI, typeof(PlaylistLoaderUI), "_loader", loader);

      // 10. PlaylistLoaderInput を UI 階層に配置
      var container = yamaPlayer.transform.Find(PlaylistLoaderInputPath);
      if (container != null)
      {
        var inputGo = CreatePlaylistLoaderInput(container);
        var urlInput = inputGo.GetComponent<VRCUrlInputField>();
        if (urlInput != null)
        {
          SetField(loaderUI, typeof(PlaylistLoaderUI), "_playlistUrlInput", urlInput);
        }
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
        "PlaylistLoader をインストールしました。\n\n次の手順:\n1. Inspector で Pool ID を確認\n2. Generate Pool を実行", "OK");
    }

    private static GameObject CreatePlaylistLoaderInput(Transform parent)
    {
      // 既存の UrlInput を参考に、PlaylistLoaderInput を動的生成
      // EventTrigger / Animator は付けない（onEndEdit で PlayUrl が呼ばれないようにするため）
      var go = new GameObject("PlaylistLoaderInput");
      go.transform.SetParent(parent, false);
      Undo.RegisterCreatedObjectUndo(go, "Create PlaylistLoaderInput");

      // RectTransform 設定 (元の UrlInput と同じサイズ、位置は下にずらす)
      var rt = go.AddComponent<RectTransform>();
      rt.anchorMin = new Vector2(0f, 1f);
      rt.anchorMax = new Vector2(0f, 1f);
      rt.anchoredPosition = new Vector2(10f, -110f);
      rt.sizeDelta = new Vector2(40f, 40f);

      // Image (背景)
      go.AddComponent<CanvasRenderer>();
      var image = go.AddComponent<Image>();
      image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

      // VRCUrlInputField
      go.AddComponent<VRCUrlInputField>();

      // Text 子オブジェクト (プレースホルダー)
      var textGo = new GameObject("Text");
      textGo.transform.SetParent(go.transform, false);
      var textRt = textGo.AddComponent<RectTransform>();
      textRt.anchorMin = Vector2.zero;
      textRt.anchorMax = Vector2.one;
      textRt.offsetMin = new Vector2(5f, 2f);
      textRt.offsetMax = new Vector2(-5f, -2f);
      textGo.AddComponent<CanvasRenderer>();
      var text = textGo.AddComponent<Text>();
      text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
      text.fontSize = 12;
      text.color = Color.white;
      text.alignment = TextAnchor.MiddleLeft;

      return go;
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
