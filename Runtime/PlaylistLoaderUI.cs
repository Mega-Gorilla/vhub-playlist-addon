using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace Yamadev.YamaStream.Modules.PlaylistLoader
{
  [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
  public class PlaylistLoaderUI : UdonSharpBehaviour
  {
    [SerializeField] private PlaylistLoader _loader;
    [SerializeField] private VRCUrlInputField _playlistUrlInput;
    [SerializeField] private Text _statusText;
    [SerializeField] private float _notificationDuration = 5f;

    public void OnPlaylistUrlSubmit()
    {
      if (!Utilities.IsValid(_playlistUrlInput) || !Utilities.IsValid(_loader)) return;
      if (_loader.IsLoading) return;
      var url = _playlistUrlInput.GetUrl();
      if (!Utilities.IsValid(url) || string.IsNullOrEmpty(url.Get())) return;
      _playlistUrlInput.SetUrl(VRCUrl.Empty);
      _loader.LoadPlaylistFromUrl(url);
    }

    public void ShowNotification(string message)
    {
      Debug.Log($"[PlaylistLoader] {message}");
      if (Utilities.IsValid(_statusText))
      {
        _statusText.text = message;
        SendCustomEventDelayedSeconds(nameof(ClearNotification), _notificationDuration);
      }
    }

    public void ClearNotification()
    {
      if (Utilities.IsValid(_statusText))
        _statusText.text = "";
    }
  }
}
