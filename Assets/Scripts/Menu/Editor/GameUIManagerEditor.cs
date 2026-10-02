using Editor;
using UnityEditor;
using UnityEngine;

namespace Menu.Editor
{
    [CustomEditor(typeof(GameUIManager))]
    public class GameUIManagerEditor : ToolEditor<GameUIManager>
    {
        protected override void OnEnableOverride()
        {
            Parameters = new[] { "UI GameObjects", "Text", "Localized Text", "Canvas Group", "Transform", "Image", "Misc" };
        }

        protected override void OnInspectorGUIOverride()
        {
            switch (Tool)
            {
                case 0: //Menu GameObjects
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("UI Objects", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Game", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gameScene)), new GUIContent("Game Object", "The default game scene"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gameSceneImportant)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gameSceneMisc)));
                    EditorGUI.indentLevel = 3;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.scope)), new GUIContent("Scope Object", "The scope object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.playersUI)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.teamPlayersUI)));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Menu", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.mainMenu)), new GUIContent("Menu Object", "The menu object"));
                    EditorGUI.indentLevel = 3;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.menuScene)), new GUIContent("Main Menu", "The menu object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.settingMenu)), new GUIContent("Setting Menu", "The Setting Menu"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.weapon)), new GUIContent("Weapon Menu", "The Weapon Menu"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.inventory)), new GUIContent("Inventory Menu", "The Inventory Menu"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.achievement)), new GUIContent("Achievement Menu", "The Achievement Menu"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.voteKickMenu)), new GUIContent("VoteKick Menu", "The VoteKick Menu"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.serverSetting)), new GUIContent("Server Setting Menu", "The Server Setting Menu"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("ScoreBoard", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.scoreBoard)), new GUIContent("ScoreBoard", "The scoreboard object"));
                    EditorGUI.indentLevel = 3;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.scoreBoardList)), new GUIContent("ScoreBoard List", "The ScoreBoard list"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Report Menu", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.reportMenu)), new GUIContent("Report Menu Object", "The Report Menu object"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Lobby", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.lobbyIdTextObj)), new GUIContent("Lobby Id Object", "The Lobby Id object"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Winner", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.winnerObj)), new GUIContent("Winner Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.firstWinner)), new GUIContent("First Winner Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.secondWinner)), new GUIContent("Second Winner Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.thirdWinner)), new GUIContent("Third Winner Object"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Mic", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.micIcon)), new GUIContent("Mic Icon Object", "The Mic Icon object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.micSpeak)), new GUIContent("Mic Speak Icon Object", "The Mic Speak Icon object"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.timeTextObj)), new GUIContent("Time Text Object", "The Time Text object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.death)), new GUIContent("Death Object", "The Death object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.crossHair)), new GUIContent("Crosshair Object", "The Crosshair object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.reloadProgressObj)), new GUIContent("Reload Progress Object", "The Reload Progress object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.dir)), new GUIContent("Direction Object", "The Direction object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.infectedCanvas)), new GUIContent("Infected Canvas Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.kick)), new GUIContent("Kick Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.ban)), new GUIContent("Ban Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.upgradeTab)), new GUIContent("Upgrade Object"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.messagePanel)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.spectateCanvas)));
                    
                    GUILayout.EndVertical();
                    break;
                case 1: //Text
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    EditorGUILayout.LabelField("Texts", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.bulletText)), new GUIContent("Bullet Text", "Show how many bullets left"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.healthText)), new GUIContent("Health Text", "Show how many health left"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.deathName)), new GUIContent("Death Name Text", "Shows the death name"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.deathTimeText)), new GUIContent("Death Time Text", "Shows the death time"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Game", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.leftTime)), new GUIContent("Left Time Text", "Show how many time left"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.leftTimeText)), new GUIContent("End Round Left Time Text", "Show how many time left"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.scoreBoardLeftTime)), new GUIContent("ScoreBoard Left Time Text", "Show how many health left but on scoreboard"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gameModeText)), new GUIContent("GameMode Text", "Shows the gamemode"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Kill Secured", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.killSecuredText)), new GUIContent("Kill Secured Text", "Shows you killed who"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Lobby", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.lobbyIdText)), new GUIContent("Lobby Id Text", "Shows the lobby id"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.profileName)), new GUIContent("Profile Name Text", "Shows the profile name"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.levelText)), new GUIContent("Profile Level Text", "Shows the profile level"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.expText)), new GUIContent("Profile Exp Text", "Shows the profile exp"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Throwables", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.throwObjCount)), new GUIContent("Throwable Count Text", "Shows how many throwables left"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.throwKeyText)), new GUIContent("Throwable Key Text", "Shows which key to throw the throwable"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Winner", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.winFirstText)), new GUIContent("Winner First"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.winSecondText)), new GUIContent("Winner Second"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.winThirdText)), new GUIContent("Winner Third"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.hitPlayer)), new GUIContent("Camera Hit Player Name Text", "Shows the player you are looking at"));
                    
                    GUILayout.EndVertical();
                    break;
                case 2: //Localized Text
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Localized Texts", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Game", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gameModeMap)), new GUIContent("Map Localized Text"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.parkourComplete)), new GUIContent("Parkour Complete Localized Text"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.message)), new GUIContent("Message Localized Text"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.deathLocalizedText)), new GUIContent("Death Localized Text"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.damageTakenText)), new GUIContent("Damage Taken Localized Text"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.damageGivenText)), new GUIContent("Damage Given Localized Text"));
                    
                    GUILayout.EndVertical();
                    break;
                case 3: //Canvas Group
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Canvas Groups", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Game", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gameGroup)), new GUIContent("Game View Group"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.hurtCanvas)), new GUIContent("Hurt Canvas Group"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.healthCanvas)), new GUIContent("Health Canvas Group"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.speedUpCanvas)), new GUIContent("Speed Up Canvas Group"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.invincibleCanvas)), new GUIContent("Invincible Canvas Group"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.fireCanvas)), new GUIContent("Fire Canvas Group"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Kill Secured", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.killSecuredCanvas)), new GUIContent("Kill Secured Canvas Group"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.parkourComplete)), new GUIContent("ParkourComplete Canvas Group"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.weaponGroup)), new GUIContent("Weapon Canvas Group"));
                    
                    GUILayout.EndVertical();
                    break;
                case 4: //Content
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Canvas Groups", EditorStyles.boldLabel);

                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.redTeamContent)), new GUIContent("Red Team Content"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.blueTeamContent)), new GUIContent("Blue Team Content"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.wishDir)), new GUIContent("Wish Dir"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.velDir)), new GUIContent("Velocity Dir"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Kill Secured", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.killSecured)), new GUIContent("Kill Secured Transform"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Misc", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.voteKickPlayerContent)), new GUIContent("VoteKick Player Content"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.playerListContent)), new GUIContent("Player List Content"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.serverManage)), new GUIContent("Player Profile Transform"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.receiveContent)), new GUIContent("Receive Content"));

                    GUILayout.EndVertical();
                    break;
                case 5: //Image
                    EditorGUI.indentLevel = 0;
                    GUILayout.BeginVertical("Box");
                    
                    EditorGUILayout.LabelField("Images", EditorStyles.boldLabel);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Game", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.afterImage)), new GUIContent("Flashbang Image"));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Player", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.healthImage)), new GUIContent("Health Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.image)), new GUIContent("Health Icon Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.throwablesFilled)), new GUIContent("Throwable Filled Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.throwObjImage)), new GUIContent("Throwable Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.deathWeapon)), new GUIContent("Death Weapon Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.deathAvatar)), new GUIContent("Death Avatar Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.reloadProgressBar)), new GUIContent("Reload Progress Image"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.levelImages)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.scoreBoardBG)));
                    
                    GUILayout.Space(Space);
                    
                    EditorGUI.indentLevel = 1;
                    EditorGUILayout.LabelField("Profile", EditorStyles.boldLabel);
                    EditorGUI.indentLevel = 2;
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.profileAvatar)), new GUIContent("Profile Avatar Image"));
                    
                    GUILayout.EndVertical();
                    break;
                case 6: //Misc
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.gradient)));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(Target.weaponUis)));
                    break;
            }
        }
    }
}