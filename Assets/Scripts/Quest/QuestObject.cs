using UnityEngine;

namespace Quest
{
    [CreateAssetMenu(fileName = "Quest", menuName = "Quest", order = 0)]
    public class QuestObject : ScriptableObject
    {
        public QuestType questType = QuestType.None;
        public string nameKey;
        public string descKey;
        public string prizeKey;
        public int requiredAmount = 1;
        public int expReward=0;
        public bool bonus = false;

        public QuestRewardType[] rewards = new QuestRewardType[1];
    }

    public enum QuestType
    {
        None,
        RifleMan,
        Match, //Play 5 matches
        BananaMan, //Kill 20 people with banana weapons
        Win, //Win 2 matches
        OpenBox,//Open 3 crates
        CompleteQuest,//Complete other quests
        GrappleMan, //Do grapple in 3 match each one should have one grapple
        Killer, //Kill 30 people in one match (bonus)
        Noob, //Dead 100 times (bonus)
        KnifeMan, //Kill 25 people with knife
        OneVsOne, //Win in a 1v1 lobby
        Flash, // Flashbang 5 people (bonus)
        EndlessKiller, //Kill 40 enemies in endless (bonus)
        Grenadier, //Use grenade kill 15 people
        EndlessKing, //Survive 400s in endless mode (bonus)
    }

    public enum QuestRewardType
    {
        None,
        Crate1
    }
}