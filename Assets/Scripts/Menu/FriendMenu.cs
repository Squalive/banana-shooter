using System.Linq;
using Manager;
using Steamworks;
using UnityEngine;

namespace Menu
{
    public class FriendMenu : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private UIHitDetection hit;

        [SerializeField] private Vector2 notDisplayPos;
        private Vector2 _desiredPos;

        [SerializeField] private FriendDetailUI prefab;
        [SerializeField] private Transform content;
        private void Awake()
        {
            _desiredPos = notDisplayPos;

            var list = FriendManager.Friends.Values.ToList();
            list.Sort((friend, friend1) => friend.PersonalState.CompareTo(friend1.PersonalState));
            list.Reverse();
            foreach (var friend in list)
            {
                if (friend.PersonalState == EPersonaState.k_EPersonaStateAway || friend.PersonalState == EPersonaState.k_EPersonaStateSnooze) continue;
                FriendDetailUI ui = Instantiate(prefab, content);
                
                ui.Initialize(friend);
            }
        }

        private void OnEnable()
        {
            hit.pointerEnter.AddListener(PointerEnter);
            hit.pointerExit.AddListener(PointerExit);
        }


        private void OnDisable()
        {
            hit.pointerEnter.RemoveAllListeners();
            hit.pointerExit.RemoveAllListeners();
        }
        
        
        private void PointerEnter()
        {
            _desiredPos=Vector2.zero;
        }
        
        private void PointerExit()
        {
            _desiredPos = notDisplayPos;
        }

        private void Update()
        {
            target.anchoredPosition = Vector2.Lerp(target.anchoredPosition,_desiredPos,Time.deltaTime*10f);
        }
    }
}
