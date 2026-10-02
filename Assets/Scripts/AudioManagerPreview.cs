#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Audio
{
    [CustomPreview(typeof(AudioManager))]
    public class AudioManagerPreview : ObjectPreview
    {
        public override bool HasPreviewGUI()
        {
            return true;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (target is not AudioManager audioManager)
                return;

            GUILayout.BeginVertical();

            if (GUILayout.Button("Check Audio Missing"))
            {
                foreach (var sound in audioManager.sounds)
                {
                    if (sound.clip == null)
                    {
                        Debug.Log($"{sound.name} is missing audio clips");
                    }
                }
            }
            
            GUILayout.EndVertical();
        }
    }
}
#endif

