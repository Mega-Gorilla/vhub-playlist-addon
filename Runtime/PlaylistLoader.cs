using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace Yamadev.YamaStream.Modules.PlaylistLoader
{
  [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
  public class PlaylistLoader : UdonSharpBehaviour
  {
    [SerializeField] private UdonBehaviour _controller;
    [SerializeField] private PlaylistLoaderUI _ui;
    [SerializeField] private VRCUrl[] _redirectPool = new VRCUrl[0];
    [SerializeField] private string _poolId = "default";
    [SerializeField] private string _poolBaseUrl = "https://playlist.vrc-hub.com";
    [SerializeField] private int _poolSize = 100000;

    private bool _isLoading;
    private VRCUrl _pendingResolveUrl;

    public VRCUrl[] RedirectPool => _redirectPool;
    public string PoolId => _poolId;
    public bool IsLoading => _isLoading;

    private void Start()
    {
      if (Utilities.IsValid(_controller)) return;

      _controller = FindControllerInParentHierarchy();
      if (!Utilities.IsValid(_controller))
      {
        PrintError("Controller が見つかりません。YamaPlayer の子階層に配置されているか確認してください。");
      }
      else
      {
        PrintLog("Controller を自動検出しました。");
      }
    }

    private UdonBehaviour FindControllerInParentHierarchy()
    {
      // PlaylistLoader は YamaPlayer の子階層に配置される前提
      // 親を遡り、その配下から Controller (= _queue を持つ UdonBehaviour) を検出
      var current = transform.parent;
      while (Utilities.IsValid(current))
      {
        var udons = current.GetComponentsInChildren<UdonBehaviour>(true);
        for (int i = 0; i < udons.Length; i++)
        {
          if (udons[i] == null) continue;
          var queue = udons[i].GetProgramVariable("_queue");
          if (queue != null) return udons[i];
        }
        current = current.parent;
      }
      return null;
    }

    public void LoadPlaylistFromUrl(VRCUrl resolveUrl)
    {
      if (_isLoading)
      {
        PrintWarning("Already loading a playlist.");
        return;
      }

      _isLoading = true;
      _pendingResolveUrl = resolveUrl;
      VRCStringDownloader.LoadUrl(resolveUrl, (IUdonEventReceiver)this);
      PrintLog($"Downloading playlist from {resolveUrl.Get()}...");
    }

    public override void OnStringLoadSuccess(IVRCStringDownload result)
    {
      if (!Utilities.IsValid(_pendingResolveUrl) ||
          result.Url.Get() != _pendingResolveUrl.Get())
        return;

      _isLoading = false;

      if (!TryParseResponse(result.Result, out DataList tracks)) return;

      var builtTracks = BuildTracks(tracks, out int failedCount);
      if (builtTracks == null) return;

      EnqueueAndPlay(builtTracks, tracks.Count, failedCount);
    }

    public override void OnStringLoadError(IVRCStringDownload result)
    {
      if (!Utilities.IsValid(_pendingResolveUrl) ||
          result.Url.Get() != _pendingResolveUrl.Get())
        return;

      _isLoading = false;
      PrintError($"Failed to download playlist: {result.Error}");
      NotifyUI("Playlist server is unavailable.");
    }

    private bool TryParseResponse(string json, out DataList tracks)
    {
      tracks = null;

      if (!VRCJson.TryDeserializeFromJson(json, out DataToken root)
          || root.TokenType != TokenType.DataDictionary)
      {
        PrintError("Failed to parse playlist response.");
        NotifyUI("Failed to parse playlist response.");
        return false;
      }

      var rootDict = root.DataDictionary;

      if (rootDict.TryGetValue("ok", out DataToken okToken)
          && okToken.TokenType == TokenType.Boolean
          && !okToken.Boolean)
      {
        string error = "Playlist server returned an error.";
        if (rootDict.TryGetValue("error", out DataToken errToken)
            && errToken.TokenType == TokenType.String)
          error = errToken.String;
        PrintError(error);
        NotifyUI(error);
        return false;
      }

      if (!rootDict.TryGetValue("tracks", out DataToken tracksToken)
          || tracksToken.TokenType != TokenType.DataList
          || tracksToken.DataList.Count == 0)
      {
        PrintWarning("No tracks found in playlist.");
        NotifyUI("No tracks found in playlist.");
        return false;
      }

      tracks = tracksToken.DataList;
      return true;
    }

    private object[][] BuildTracks(DataList trackDicts, out int failedCount)
    {
      failedCount = 0;
      int totalCount = trackDicts.Count;
      var tempTracks = new object[totalCount][];
      int addedCount = 0;

      for (int i = 0; i < totalCount; i++)
      {
        if (trackDicts[i].TokenType != TokenType.DataDictionary)
        {
          failedCount++;
          continue;
        }
        var dict = trackDicts[i].DataDictionary;

        int index = TryGetInt(dict, "index", -1);
        if (index < 0 || index >= _redirectPool.Length)
        {
          failedCount++;
          continue;
        }

        int mode = TryGetInt(dict, "mode", 0);
        string title = "";
        if (dict.TryGetValue("title", out DataToken t)
            && t.TokenType == TokenType.String)
          title = t.String;

        // track format: [VideoPlayerType(int), title, VRCUrl]
        tempTracks[addedCount] = new object[] { mode, title, _redirectPool[index] };
        addedCount++;
      }

      if (addedCount == 0)
      {
        string msg = failedCount > 0
            ? $"No tracks could be added ({failedCount} skipped)"
            : "No valid tracks to add.";
        PrintWarning(msg);
        NotifyUI(msg);
        return null;
      }

      var result = new object[addedCount][];
      for (int i = 0; i < addedCount; i++) result[i] = tempTracks[i];
      return result;
    }

    private void EnqueueAndPlay(object[][] tracks, int totalCount, int failedCount)
    {
      if (!Utilities.IsValid(_controller))
      {
        PrintError("Controller is not available.");
        NotifyUI("Controller is not available.");
        return;
      }

      var queue = (UdonBehaviour)_controller.GetProgramVariable("_queue");
      if (!Utilities.IsValid(queue))
      {
        PrintError("Queue is not available.");
        NotifyUI("Queue is not available.");
        return;
      }

      _controller.SendCustomEvent("TakeOwnership");
      Networking.SetOwner(Networking.LocalPlayer, queue.gameObject);

      for (int i = 0; i < tracks.Length; i++)
      {
        AddTrackToQueue(queue, tracks[i]);
      }

      // 自動再生: Idle(0) の場合のみ Forward で再生開始
      int state = (int)_controller.GetProgramVariable("_syncedState");
      if (state == 0)
      {
        _controller.SendCustomEvent("Forward");
      }

      var message = failedCount > 0
          ? $"Added {tracks.Length}/{totalCount} tracks ({failedCount} failed)"
          : $"Added {tracks.Length} tracks to queue";
      PrintLog(message);
      NotifyUI(message);
    }

    private void AddTrackToQueue(UdonBehaviour queue, object[] track)
    {
      object[][] currentTracks = (object[][])queue.GetProgramVariable("_tracks");
      int len = currentTracks != null ? currentTracks.Length : 0;
      object[][] newTracks = new object[len + 1][];
      for (int i = 0; i < len; i++) newTracks[i] = currentTracks[i];
      newTracks[len] = track;
      queue.SetProgramVariable("_tracks", newTracks);
    }

    private void NotifyUI(string message)
    {
      if (Utilities.IsValid(_ui)) _ui.ShowNotification(message);
    }

    private int TryGetInt(DataDictionary dict, string key, int defaultValue)
    {
      if (!dict.TryGetValue(key, out DataToken token)) return defaultValue;
      if (token.TokenType == TokenType.Double) return (int)token.Double;
      if (token.TokenType == TokenType.Float) return (int)token.Float;
      if (token.TokenType == TokenType.Int) return token.Int;
      if (token.TokenType == TokenType.Long) return (int)token.Long;
      return defaultValue;
    }

    private void PrintLog(string msg) => Debug.Log($"[PlaylistLoader] {msg}");
    private void PrintWarning(string msg) => Debug.LogWarning($"[PlaylistLoader] {msg}");
    private void PrintError(string msg) => Debug.LogError($"[PlaylistLoader] {msg}");
  }
}
