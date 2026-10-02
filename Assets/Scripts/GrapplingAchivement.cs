
using Manager;
using Movement;
using Steamworks;
using UnityEngine;

public class GrapplingAchivement : MonoBehaviour
{
    public static GrapplingAchivement Instance;

    public int maxGrapplePoint;

    public int currentGrapplePoint;

    private PlayerMovement _playerMovement;

    private void Awake()
    {
        Instance = this;
    }

    public bool flag = false;
    private void Update()
    {
        if (flag) return;
        if (!_playerMovement)
        {
            _playerMovement = PlayerMovement.Instance;
            return;
        }
        if (currentGrapplePoint != 0)
        {
            if (_playerMovement.IsGrounded())
            {
                currentGrapplePoint = 0;
            }
        }

        if (currentGrapplePoint >= maxGrapplePoint)
        {
            currentGrapplePoint = 0;
            flag = true;
            AchievementManager.Instance.SetAchievement(AchievementManager.EAchievements.I_AM_GOOT_AT_GRAPPLING);
        }
    }
}
