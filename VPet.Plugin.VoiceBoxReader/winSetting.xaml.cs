using LinePutScript.Converter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VPet.Plugin.VPet_VoiceBox;
using VPet_Simulator.Core;

namespace VPet.Plugin.VPet_VoiceBox
{
    /// <summary>
    /// winSetting.xaml の相互作用ロジック
    /// </summary>
    public partial class winSetting : Window
    {
        private bool AllowChange = false;
        VoiceBoxReader vboxr;
        Setting Set;
        List<Speaker> speakers;
        Speaker selectedSpeaker;

        public winSetting(VoiceBoxReader vboxr)
        {
            InitializeComponent();
            this.vboxr = vboxr;
            this.Set = vboxr.Set;
            Console.WriteLine(vboxr);
            SwitchOn.IsChecked = vboxr.Set.Enable;
            VoiceBoxEnginUri.Text = vboxr.Set.VoiceBoxEngineUri;
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            speakers = await vboxr.voiceBoxClient.GetSpeakers();
            foreach (var speaker in speakers)
            {
                charComboBox.Items.Add(speaker.Name);
            }
        }

        public void CharComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            styleComboBox.Items.Clear(); // 一旦クリアしてから追加

            string selectedCharIndex = charComboBox.SelectedValue.ToString();

            foreach (var speaker in speakers)
            {
                if (speaker.Name == selectedCharIndex)
                {
                    selectedSpeaker = speaker;
                    break;
                }
                
            }

            if (selectedSpeaker == null) return;

            foreach (var style in selectedSpeaker.Styles)
            {
                styleComboBox.Items.Add(style.Name);
            }

        }

        public void VoiceBoxEngineUriDefault_Click(object sender, RoutedEventArgs e)
        {
            vboxr.Set.VoiceBoxEngineUri = "http://127.0.0.1:50021";
            VoiceBoxEnginUri.Text = vboxr.Set.VoiceBoxEngineUri;
        }

        private void StyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            string selectedStyleName = styleComboBox.SelectedValue.ToString();
            
            foreach (var style in selectedSpeaker.Styles)
            {
                if (style.Name == selectedStyleName)
                {
                    vboxr.Set.Speaker = style.Id;
                }
            }
        }

        public void Save_Click(object sender, RoutedEventArgs e)
        {
            if (vboxr.Set.Enable != SwitchOn.IsChecked.Value)
            {
                if (SwitchOn.IsChecked.Value)
                    vboxr.MW.Main.OnSay += vboxr.Main_OnSay;
                else
                    vboxr.MW.Main.OnSay -= vboxr.Main_OnSay;
                vboxr.Set.Enable = SwitchOn.IsChecked.Value;
            }

            vboxr.Set.VoiceBoxEngineUri = VoiceBoxEnginUri.Text;
            vboxr.voiceBoxClient.BaseUri = vboxr.Set.VoiceBoxEngineUri;  // VoiceBox ClientのUriも変更する
            vboxr.Set.VolumeScale = VolumeSilder.Value;
            vboxr.Set.Speaker = vboxr.Set.Speaker;

            vboxr.SaveSetting();
            foreach (var tmpfile in Directory.GetFiles($"{GraphCore.CachePath}\\voice"))
            {
                try
                {
                    File.Delete(tmpfile);
                } finally
                {

                }
            }
            Close();

        }

        public void Switch_Checked(object sender, RoutedEventArgs e)
        {
            if (!AllowChange)
                return;
            Set.Enable = SwitchOn.IsChecked.Value;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            vboxr.winSetting = null;
        }
    }
}
