using System;
using System.Collections;
using System.Collections.Generic;
using Cosmetic;
using Manager;
using Menu;
using Movement;
using Multiplayer;
using Multiplayer.Entity.Client;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utils;

namespace Console
{
    public class DeveloperConsoleUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField infoText;
        [SerializeField] private ConsoleCommand[] commands = new ConsoleCommand[0];
        [SerializeField] public GameObject uiCanvas;
        [SerializeField] private TMP_InputField inputField;

        public static DeveloperConsoleUI Instance;

        private DeveloperConsole developerConsole;

        [SerializeField] public Transform tipContent;//CommandTipItem

        public bool enableLog = true,enableError=true,enableWarning=true;
        public DeveloperConsole DeveloperConsole
        {
            get
            {
                if (developerConsole != null) return developerConsole;
                return developerConsole = new DeveloperConsole(commands);
            }
        }

        private InputManager _inputManager;

        public CosmeticItem[] crates = Array.Empty<CosmeticItem>();

        private bool _rewinding = false;

        private int _rewindIndex = -1;

        private List<string> _rewindCommands = new List<string>();
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _inputManager ??= new InputManager();
                _inputManager.Enable();
                inputField.onValueChanged.AddListener(CheckInput);

#if UNITY_EDITOR
                Debug.unityLogger.logEnabled = true;
#else
                Debug.unityLogger.logEnabled = true;
#endif
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InvokeRepeating(nameof(RefreshInfo), 0, 0.5f);
        }

        private void OnEnable()
        {
            if (Instance == this)
            {
                _inputManager.Console.Toggle.performed += Toggle;
                _inputManager.Console.Process.performed += Process;
                _inputManager.Console.Next.performed += NextIndex;
                _inputManager.Console.Preview.performed += PreviewIndex;
                _inputManager.Console.QuickTip.performed += QuickTip;

                Application.logMessageReceived += HandleLog;
            }
        }
    
        void OnDisable()
        {
            _inputManager.Console.Toggle.performed -= Toggle;
            _inputManager.Console.Process.performed -= Process;
            _inputManager.Console.Next.performed -= NextIndex;
            _inputManager.Console.Preview.performed -= PreviewIndex;
            _inputManager.Console.QuickTip.performed -= QuickTip;
            Application.logMessageReceived -= HandleLog;
            _inputManager.Disable();
        }

        void RefreshInfo()
        {
            ComputerDetails.Refresh();
            infoText.SetTextWithoutNotify(ComputerDetails.GetDetails());
        }
        
        private void QuickTip(InputAction.CallbackContext obj)
        {
            if (cmds.Count <= 0)
            {
                toggles.Clear();
                cmds.Clear();
                cmdIndex = 0;
                for (int i = 0; i < tipContent.childCount; i++)
                {
                    Destroy(tipContent.GetChild(i).gameObject);
                }

                return;
            }

            string cmd = cmds[cmdIndex];
        
            for (int i = 0; i < tipContent.childCount; i++)
            {
                Destroy(tipContent.GetChild(i).gameObject);
            }

            toggles.Clear();
            cmds.Clear();
            cmdIndex = 0;

            string[] sp = inputField.text.Split(' ');
            sp[^1] = cmd;

            string text = string.Join(' ', sp);
            inputField.text = text;

            StartCoroutine(MoveCursorToEnd());
        }

        IEnumerator MoveCursorToEnd()
        {
            yield return new WaitForEndOfFrame();
            inputField.caretPosition = inputField.text.Length ;
            inputField.ForceLabelUpdate();
        }
        private void PreviewIndex(InputAction.CallbackContext obj)
        {
            if (!uiCanvas.activeSelf) return;
            
            if ((inputField.text.Length <= 0 && !_rewinding) || _rewinding)
            {
                _rewinding = true;

                _rewindIndex--;
                
                ProcessRewind();
            }
            
            cmdIndex--;
            if (cmdIndex < 0) cmdIndex = toggles.Count-1;
        
            if(toggles.Count>0)
                toggles[cmdIndex].isOn = true;
            StartCoroutine(MoveCursorToEnd());
        }
    
        private void NextIndex(InputAction.CallbackContext obj)
        {
            if (!uiCanvas.activeSelf) return;
            
            if ((inputField.text.Length <= 0 && !_rewinding) || _rewinding)
            {
                _rewinding = true;

                _rewindIndex++;

                ProcessRewind();
            }
        
            cmdIndex++;
            if (cmdIndex > toggles.Count - 1) cmdIndex = 0;

            if(toggles.Count>0)
                toggles[cmdIndex].isOn = true;
            StartCoroutine(MoveCursorToEnd());
        }

        void ProcessRewind()
        {
            if (_rewindIndex < 0) _rewindIndex = _rewindCommands.Count - 1;
            else if (_rewindIndex >= _rewindCommands.Count) _rewindIndex = 0;

            if (_rewindCommands.Count <= 0) return;
            
            inputField.SetTextWithoutNotify(_rewindCommands[_rewindIndex]);
        }

        private void Process(InputAction.CallbackContext obj)
        {
            if (uiCanvas.activeSelf)
            {
                _rewindIndex = -1;
                _rewindCommands.Add(inputField.text);

                if (_rewindCommands.Count >= 20)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        _rewindCommands.RemoveAt(i);
                    }
                }
                
                DeveloperConsole.ProcessCommand(inputField.text);
                inputField.text = String.Empty;
                inputField.ActivateInputField();
            }
        }

        public void Close()
        {
            uiCanvas.SetActive(false);
            if ((GameUIManager.Instance && !GameUIManager.Instance.pause) 
                && (!EndScreenUI.Instance.endScreen.activeSelf)
                &&(TeamSelector.Instance && !TeamSelector.Instance.IsSelecting))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private void Toggle(InputAction.CallbackContext obj)
        {
            if (uiCanvas.activeSelf)
            {
                Close();
            }
            else
            {
                uiCanvas.SetActive(true);
                inputField.ActivateInputField();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                inputField.text = String.Empty;
            }
        }

    
        void HandleLog(string logMessage, string stackTrace, LogType type)
        {
            string _message= string.Empty;
            switch (type)
            {
                case LogType.Log:
                    if (enableLog)
                        _message = logMessage;
                    else
                        return;
                    break;
                case LogType.Error:
                    if (enableError)
                        _message = "<color=red>"+ logMessage + "</color>";
                    else
                        return;
                    break;
                case LogType.Warning:
                    if (enableWarning)
                        _message = "<color=yellow>" + logMessage + "</color>";
                    else
                        return;
                    break;
            }

            if(!string.IsNullOrEmpty(_message))
                AddMessageToConsole(_message);

        }

        // private int loopCount = 0;
        // string lastText = String.Empty;
        // private TextMeshProUGUI lastTextUI;

        [SerializeField] private TMP_InputField text;

        public void AddMessageToConsole(string m)
        {
            if (text.text.Length > 10000)
            {
                text.text = String.Empty;
            }
            text.text += m + "\n";
        }

        public void ClearConsole()
        {
            text.text = String.Empty;
        }
        List<string> list = new List<string>();
        private List<Toggle> toggles = new List<Toggle>();
        private List<string> cmds = new List<string>();
        private int cmdIndex = 0;
        [SerializeField] private ToggleGroup group;
        void CheckInput(string s)
        {
            _rewinding = false;
            int i;
            for (i = 0; i < tipContent.childCount; i++)
            {
                Destroy(tipContent.GetChild(i).gameObject);
            }

            toggles.Clear();
            cmds.Clear();
            cmdIndex = 0;

            if (string.IsNullOrEmpty(s)) return;
            string[] args = s.Split(' ');

            list.Clear();
            foreach (var arg in args)
            {
                if (!string.IsNullOrEmpty(arg))
                {
                    list.Add(arg);
                }
            }
            if(string.IsNullOrEmpty(args[^1]))
                list.Add("");

            args = list.ToArray();

        
            if (args.Length <= 1)
            {
                foreach (var command in commands)
                {
                    SpawnTip(command.CommandWord, args[0], command.ArgsHint, command.Description);
                    
                    // if (command.CommandWord.Contains(args[0]) && args[0] != command.CommandWord) 
                    // {
                    //     Toggle toggle = Instantiate(PrefabManager.Instance.GetPrefab("CommandTipItem"), tipContent).GetComponent<Toggle>();
                    //     toggle.GetComponentInChildren<TextMeshProUGUI>().SetText(command.CommandWord);
                    //     toggle.group = group;
                    //     toggles.Add(toggle);
                    //     cmds.Add(command.CommandWord);
                    // }
                }
            
            }
            else if (args.Length >= 2)
            {
                foreach (var command in commands)
                {
                    if (command.CommandWord == args[0])
                    {
                        int index = args.Length - 1;
                        if (index - 1 < command.key.Count)
                        {
                            string tip = String.Empty;
                            switch (command.key[index - 1])
                            {
                                case "player":
                                    foreach (var player in ClientPlayer.list.Values)
                                    {
                                        if (player.playerState.Username.Contains(args[^1]) && player.playerState.Username != args[^1])
                                        {
                                            Toggle toggle =
                                                Instantiate(PrefabManager.Instance.GetPrefab("CommandTipItem"),
                                                        tipContent)
                                                    .GetComponent<Toggle>();
                                            toggle.GetComponentInChildren<TextMeshProUGUI>().SetText(player.playerState.Username);
                                            toggle.group = group;
                                            toggles.Add(toggle);
                                            cmds.Add(player.playerState.Username);
                                        }
                                    }

                                    break;
                                case "normal":
                                    // int index = list.Count != args.Length ? list.Count - 2 : args.Length - 2;
                                    if (command.args.Count > 0)
                                    {
                                        foreach (var arg in command.args[args.Length - 2].args)
                                        {
                                            SpawnTip(arg,args[^1]);
                                            // if (arg.ToLower().Contains(args[^1].ToLower()) && arg != args[^1])
                                            // {
                                            //     Toggle toggle =
                                            //         Instantiate(PrefabManager.Instance.GetPrefab("CommandTipItem"),
                                            //                 tipContent)
                                            //             .GetComponent<Toggle>();
                                            //     toggle.GetComponentInChildren<TextMeshProUGUI>().SetText(arg);
                                            //     toggle.group = group;
                                            //     toggles.Add(toggle);
                                            //     cmds.Add(arg);
                                            // }
                                        }
                                    }

                                    break;
                                case "bindings":
                                    foreach (var key in GameManager.CustomInputActions.Keys)
                                    {
                                        SpawnTip(key,args[^1]);
                                    }
                                    break;
                                case "crate":
                                    foreach (var crate in crates)
                                    {
                                        if (crate.name.ToLower().Contains(args[^1].ToLower()) && crate.name != args[^1])
                                        {
                                            Toggle toggle =
                                                Instantiate(PrefabManager.Instance.GetPrefab("CommandTipItem"),
                                                        tipContent)
                                                    .GetComponent<Toggle>();
                                            toggle.GetComponentInChildren<TextMeshProUGUI>().SetText(crate.name);
                                            toggle.group = group;
                                            toggles.Add(toggle);
                                            cmds.Add(crate.name);
                                        }
                                    }

                                    break;
                                case "intro":
                                    tip = GameManager.Instance.introTheme.ToString();
                                    break;
                                case "displaytag":
                                    tip = NetworkManager.Instance.displayTag.ToString().ToLower();
                                    break;
                                case "fps":
                                    tip = Fps.Instance.enable.ToString().ToLower();
                                    break;
                                case "steamlanguage":
                                    tip = GameManager.Instance.setting.useSteamLanguage.ToString().ToLower();
                                    break;
                                case "crate_open_animation_enable":
                                    tip = InventoryManager.Instance.CrateOpenAnimationEnable.ToString().ToLower();
                                    break;
                                case "sv_cheats":
                                    tip = NetworkServerManager.Instance.Server.IsRunning ? NetworkServerManager.CheatsEnabled.ToString().ToLower() : NetworkManager.ClientCheatsEnabled.ToString().ToLower();
                                    break;
                                case "last_command":
                                    string sKey = args[^2];

                                    switch (sKey)
                                    {
                                        case "log":
                                            tip = enableLog.ToString().ToLower();
                                            break;
                                        case "error":
                                            tip = enableError.ToString().ToLower();
                                            break;
                                        case "warning":
                                            tip = enableWarning.ToString().ToLower();
                                            break;
                                        case "network":
                                            tip = Fps.Instance.enableNetworkingStats.ToString().ToLower();
                                            break;
                                        case "memory":
                                            tip = Fps.Instance.enableMemoryStatics.ToString().ToLower();
                                            break;
                                        case "cammovespeed":
                                            tip = SpectateMovement.Instance.moveSpeedMultiplier.ToString("F1").ToLower();
                                            break;
                                        case "camlerp":
                                            tip = SpectateMovement.Instance.lerpAmount.ToString("F1").ToLower();
                                            break;
                                        case "camzoomspeed":
                                            tip = SpectateMovement.Instance.zoomSpeedMultiplier.ToString("F1").ToLower();
                                            break;
                                        case "camzoomlerp":
                                            tip = SpectateMovement.Instance.zoomLerpAmount.ToString("F1").ToLower();
                                            break;
                                        case "camlock":
                                            tip = SpectateMovement.Instance.Locked.ToString().ToLower();
                                            break;
                                        case "dof":
                                            tip = SpectateMovement.Instance.GetDof().ToString().ToLower();
                                            break;
                                        case "aper":
                                            tip = SpectateMovement.Instance.GetDofAperture().ToString("F1").ToLower();
                                            break;
                                        case "blur":
                                            tip = SpectateMovement.Instance.GetDofBlur().ToString("F1").ToLower();
                                            break;
                                        case "mode":
                                            tip = SpectateMovement.Instance.GetDofMode().ToString().ToLower();
                                            break;
                                        case "distance":
                                            tip = SpectateMovement.Instance.GetDofDistance().ToString("F1").ToLower();
                                            break;
                                        case "bonerotation":
                                            tip = SpectateMovement.Instance.TargetRotationSync.ToString().ToLower();
                                            break;
                                        case "set_bone":
                                            if (SpectateMovement.Instance.Target != null)
                                            {
                                                foreach (var bone in SpectateMovement.Instance.Target.Bones)
                                                {
                                                    SpawnTip(bone.name.ToLower(),args[^1]);
                                                }
                                            }
                                            
                                            break;
                                    }

                                    break;
                            }

                            if (!string.IsNullOrEmpty(tip))
                            {
                                if (string.IsNullOrEmpty(args[^1]))
                                {
                                    SpawnTip(tip, args[^1]);
                                }
                                else
                                {
                                    foreach (var arg in command.args[args.Length - 2].args)
                                    {
                                        SpawnTip(arg,args[^1]);
                                    }
                                }
                            }
                        }
                        

                        break;
                    }
                }
            }
        
            if(toggles.Count>0)
                toggles[cmdIndex].isOn = true;
        }

        void SpawnTip(string tip,string arg,string argsHint="",string desc="")
        {
            string de = "<color=#01c600>" + argsHint + "</color>";
            if (!string.IsNullOrEmpty(desc)) de += " - " + "<color=#eac300>" + desc + "</color>";
            if (tip.Contains(arg.ToLower()) && tip != arg)
            {
                Toggle toggle =
                    Instantiate(PrefabManager.Instance.GetPrefab("CommandTipItem"), tipContent)
                        .GetComponent<Toggle>();
                toggle.GetComponentInChildren<TextMeshProUGUI>().SetText(tip + " " + de);
                toggle.group = group;
                toggles.Add(toggle);
                cmds.Add(tip);
            }
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && uiCanvas.activeSelf)
            {
                float speed = 4f;
                float input = Input.GetAxis("Mouse ScrollWheel") * speed;

                text.pointSize += input;
            }
            
        }
    }
}
