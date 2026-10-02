
using Manager;
using Map;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AmbientParticle : MonoBehaviour
{
    [SerializeField] GameObject rainFallParticle,snowParticle;

    private void Start()
    {
        if (GameManager.Instance.setting.spawnParticle)
        {
            switch (MapManager.Instance.GetMapExternal(SceneManager.GetActiveScene().name))
            {
                case MapExternal.Rainy:
                    rainFallParticle.SetActive(true);
                    break;
                case MapExternal.Snowy:
                    snowParticle.SetActive(true);
                    break;
            }
        }
    }
}