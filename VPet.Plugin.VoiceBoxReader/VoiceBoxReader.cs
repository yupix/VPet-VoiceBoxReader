using LinePutScript;
using LinePutScript.Converter;
using System;
using System.IO;
using System.Windows.Controls;
using VPet_Simulator.Core;
using VPet_Simulator.Windows.Interface;

namespace VPet.Plugin.VPet_VoiceBox
{
    public class VoiceBoxReader: MainPlugin
    {
        public Setting Set;
        public VoiceBoxClient voiceBoxClient;
        public VoiceBoxReader(IMainWindow mainwin) : base(mainwin)
        {
        }

        public override void LoadPlugin()
        {
            
            var line = MW.Set.FindLine("VoiceBoxReader");
            if (line == null)
            {
                Set = new Setting();
            } else
            {
                Set = LPSConvert.DeserializeObject<Setting>(line);
            }
            voiceBoxClient = new VoiceBoxClient(this, Set.VoiceBoxEngineUri);
            if (!Directory.Exists(GraphCore.CachePath + @"\voice"))
                Directory.CreateDirectory(GraphCore.CachePath + @"\voice");
            if (Set.Enable)
                MW.Main.OnSay += Main_OnSay;

            MenuItem modset = MW.Main.ToolBar.MenuMODConfig;
            modset.Visibility = System.Windows.Visibility.Visible;
            var menuItem = new MenuItem()
            {
                Header = "VoiceBox Reader",
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
            };

            menuItem.Click += (s, e) => { Setting(); };
            modset.Items.Add(menuItem);
        }

        public void SaveSetting()
        {
            MW.Set.Remove("VoiceBoxReader");
            MW.Set.Add(LPSConvert.SerializeObject(Set, "VoiceBoxReader"));
        }

        public override void Setting()
        {
            if (winSetting == null)
            {
                winSetting = new winSetting(this);
                winSetting.Show();
            } else
            {
                winSetting.Topmost = true;
            }
        }

        public void Main_OnSay(string message)
        {
            var path = $"{GraphCore.CachePath}\\voice\\{Sub.GetHashCode(message):X}.mp3";
            if (File.Exists(path))
            {
                MW.Main.PlayVoice(new Uri(path));
            } else
            {
                _ = voiceBoxClient.Make_Sound(message, true, Set.Speaker, Set.VolumeScale).Result;
                MW.Main.PlayVoice(new Uri(path));
            }

        }

        public winSetting winSetting;

        public override string PluginName => "VPet VoiceBox";
    }

}
