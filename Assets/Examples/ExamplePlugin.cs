using System.Collections.Generic;

using VTS.Networking.Impl;
using VTS.Models.Impl;
using VTS.Models;

using UnityEngine;
using UnityEngine.UI;

using UnityEditor;

namespace VTS.Examples
{

    public class ExamplePlugin : VTSPlugin
    {
        [SerializeField]
        private Text info = null;
        [SerializeField]
        private Text show_danmu = null;

        [SerializeField]
        private Color _color = Color.black;

        [SerializeField]
        private Button _portConnectButtonPrefab = null;

        [SerializeField]
        private RectTransform _portConnectButtonParent = null;

        [SerializeField]
        private Image _connectionLight = null;
        [SerializeField]
        private Text _connectionText = null;

        [SerializeField]
        private Dropdown hotkeyDropdown = null;
        [SerializeField]
        private Dropdown liwuDropdown = null;
        [SerializeField]
        private Dropdown SCDropdown = null;
        [SerializeField]
        private Dropdown captainList = null;

        private List<HotkeyData> hotkeys = null;

        private readonly Queue<string> danmumen = new Queue<string>();
        private int guazi = int.MaxValue;
        private string superchatKeyword = "";


        private void Awake()
        {
            Connect();
            //UnityEngine.Debug.Log(AppData);
            //UnityEngine.Debug.Log(Application.dataPath);
            Application.targetFrameRate = 30;
        }
        private void Connect()
        {
            this._connectionLight.color = Color.yellow;
            this._connectionText.text = "Connecting...";
            Initialize(new WebSocketSharpImpl(), new JsonUtilityImpl(), new TokenStorageImpl(),
            () => {
                UnityEngine.Debug.Log("Connected!");
                this._connectionLight.color = Color.green;
                this._connectionText.text = "Connected!";
            },
            () => {
                UnityEngine.Debug.LogWarning("Disconnected!");
                this._connectionLight.color = Color.gray;
                this._connectionText.text = "Disconnected.";
            },
            () => {
                UnityEngine.Debug.LogError("Error!");
                this._connectionLight.color = Color.red;
                this._connectionText.text = "Error!";
            });
        }


        public void PrintCurentModelHotkeys()
        {
            GetHotkeysInCurrentModel(
                null,
                (r) => {
                    // info.text = new JsonUtilityImpl().ToJson(r);
                    hotkeys = new List<HotkeyData>(r.data.availableHotkeys);
                },
                (e) => { info.text = e.data.message; }
            );
            if (hotkeys.Count == 0)info.text = "没有快捷键";
            else info.text = $"{hotkeys.Count}个快捷键已导入";

            hotkeyDropdown.options.Clear();
            liwuDropdown.options.Clear();
            SCDropdown.options.Clear();
            captainList.options.Clear();

            hotkeyDropdown.options.Add(new Dropdown.OptionData() { text = "无动作" });
            liwuDropdown.options.Add(new Dropdown.OptionData() { text = "无动作" });
            SCDropdown.options.Add(new Dropdown.OptionData() { text = "无动作" });
            captainList.options.Add(new Dropdown.OptionData() { text = "无动作" });

            hotkeyDropdown.value = 0;
            liwuDropdown.value = 0;
            SCDropdown.value = 0;
            captainList.value = 0;
            foreach (var hotkey in hotkeys)
            {
                Debug.Log(hotkey.file);
                hotkeyDropdown.options.Add(new Dropdown.OptionData() { text = hotkey.file });
                liwuDropdown.options.Add(new Dropdown.OptionData() { text = hotkey.file });
                SCDropdown.options.Add(new Dropdown.OptionData() { text = hotkey.file });
                captainList.options.Add(new Dropdown.OptionData() { text = hotkey.file });
            }            
        }

        public void TestHotkey()
        {
            var testedHotkey = TriggerSelectedHotkey(hotkeyDropdown);
            info.text = $"触发热键{testedHotkey.name}({testedHotkey.file})";
        }
        private HotkeyData TriggerSelectedHotkey(Dropdown dp)
        {
            string currentHotkeySelected = dp.options[dp.value].text;
            foreach (var hotkey in hotkeys)
            {
                if (hotkey.file == currentHotkeySelected)
                {
                    TriggerHotkey(hotkey.hotkeyID,
                        (r) => { },
                        e => { info.text = $"热键不存在，请刷新"; }
                        );
                    return hotkey;
                }
            }
            return null;
        }

        private void SyncValues(VTSParameterInjectionValue[] values)
        {
            InjectParameterValues(
                values,
                (r) => { },
                (e) => { print(e.data.message); }
            );
        }

        public void RefreshPortList()
        {
            List<int> ports = new List<int>(GetPorts().Keys);
            foreach (Transform child in this._portConnectButtonParent)
            {
                Destroy(child.gameObject);
            }
            foreach (int port in ports)
            {
                Button button = Instantiate<Button>(this._portConnectButtonPrefab, Vector3.zero, Quaternion.identity, this._portConnectButtonParent);
                button.name = port.ToString();
                button.GetComponentInChildren<Text>().text = button.name;
                button.onClick.AddListener(() => {
                    if (SetPort(int.Parse(button.name)))
                    {
                        Connect();
                    }
                });
            }
        }

        public void ChangeGuazi(string msg)
        {
            if (msg == "")
            {
                guazi = int.MaxValue;
                info.text = "取消金瓜子设定";
            }
            else
            {
                int.TryParse(msg, out guazi);
                info.text = $"礼物阈值：{guazi}金瓜子";
            }
        }

        public void ChangeKeyword(string msg)
        {
            superchatKeyword = msg;
            info.text = $"SuperChat关键词：{superchatKeyword}";
        }

        // 人气  f'R[{client.room_id}] 当前人气: {message.popularity}'
        // 弹幕  f'D[{client.room_id}] {message.uname}: {message.msg}'
        // 礼物  f'G[{client.room_id}] {message.uname} 赠送了 {message.gift_name}x{message.num}'
        //                         f' ({message.coin_type} 瓜子 x {message.total_coin})'
        // 舰长  f'J[{client.room_id}] {message.username} 购买了 {message.gift_name}'
        // 艾西  f'S[{client.room_id}] 醒目留言 ￥{message.price} {message.uname}：{message.message}'
        public void receiveDanmu(string message) => danmumen.Enqueue(message);

        public void FixedUpdate()
        {
            while(danmumen.Count > 0)
            {
                string danmu = danmumen.Dequeue();
                string[] danmu_msg = danmu.Split("$#**#$");
                Debug.Log(string.Join(",", danmu_msg));
                Debug.Log(danmu[0]);
                switch(danmu[0])
                {
                    // 收到弹幕
                    case 'D':
                        show_danmu.text = $"D[{danmu_msg[0]}] {danmu_msg[1]}: {danmu_msg[2]}";
                        break;

                    // 收到礼物
                    case 'G':
                        show_danmu.text = $"G[{danmu_msg[0]}] {danmu_msg[1]} 赠送了 {danmu_msg[2]}x{danmu_msg[3]}"
                                        + $" ({danmu_msg[4]} 瓜子 x {danmu_msg[5]})";
                        if (danmu_msg[4] == "gold" && int.Parse(danmu_msg[5]) >= guazi)
                        {
                            HotkeyData giftTrigger = TriggerSelectedHotkey(liwuDropdown);
                            info.text = $"{danmu_msg[1]} 的礼物触发了 {giftTrigger.name}({giftTrigger.file})";
                        }
                        break;

                    // 有人上舰
                    case 'J':
                        show_danmu.text = $"J[{danmu_msg[0]}] {danmu_msg[1]} 购买了 {danmu_msg[2]}";
                        HotkeyData captainTrigger = TriggerSelectedHotkey(captainList);
                        info.text = $"{danmu_msg[1]} 的礼物触发了 {captainTrigger.name}({captainTrigger.file})";
                        break;

                    // 速帕恰
                    case 'S':
                        show_danmu.text = $"S[{danmu_msg[0]}] 发送了醒目留言 ￥{danmu_msg[1]} {danmu_msg[2]}：{danmu_msg[3]}";
                        if (danmu_msg[3].Contains(superchatKeyword))
                        {
                            HotkeyData SCTrigger = TriggerSelectedHotkey(SCDropdown);
                            info.text = $"{danmu_msg[1]} 的礼物触发了 {SCTrigger.name}({SCTrigger.file})";
                        }
                        break;
                }
            }
        }
    }

}
