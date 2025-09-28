using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;


namespace ToDoList {
    
    public partial class Settings : Page {
        
        //init
        private TdSettingsLogic _tdProgramm;
        MainWindow _mainWindow = Application.Current.MainWindow as MainWindow;

        public Settings() {
            InitializeComponent();

            _tdProgramm = new TdSettingsLogic(_mainWindow, this);

            _tdProgramm.SyncAndLoad();
            _mainWindow.FilePathChanged = false;
        }


        //events
        private void GUIs_saveAndGoBackButton_Click(object sender, RoutedEventArgs e) {
            if(_mainWindow == null) { Application.Current.Shutdown(); }

            _tdProgramm.SaveAndReturn();
            _mainWindow.SyncSettingChanges();


            SettingsWindowGrid.Visibility = Visibility.Collapsed;
            _mainWindow.MainWindowGrid.Visibility = Visibility.Visible;
        }

        private void GUIs_fontSizeMinusButton_Click(object sender, RoutedEventArgs e) {
            int val = int.Parse(GUIs_fontSizeTextBox.Text);

            if (val == 5) { return; }

            GUIs_fontSizeTextBox.Text = $"{val - 1}";
        }

        private void GUIs_fontSizePlusButton_Click(object sender, RoutedEventArgs e) {
            int val = int.Parse(GUIs_fontSizeTextBox.Text);

            if(val == 20) { return; }

            GUIs_fontSizeTextBox.Text = $"{val + 1}";
        }

        private void GUIs_openSettingsFileButton_Click(object sender, RoutedEventArgs e) {

            MessageBox.Show("DONT EDIT 'SettingsSave.txt' OR IT MAY LEAD TO MASSIVE ERROR´S", "CAUTION!!");

            if (GUIs_openSettingsFileWithAppComboBox.SelectedIndex == 0) {
                Process.Start(_mainWindow.SettingsFilePath);
                return;
            }

            Process.Start($"{GUIs_openSettingsFileWithAppComboBox.SelectionBoxItem.ToString()}.exe", _mainWindow.SettingsFilePath);
        }

        private void GUIs_copySettingsFilePathButton_Click(object sender, RoutedEventArgs e) {
            Clipboard.SetText(_mainWindow.SettingsFilePath);
            GUIs_copySettingsFilePathButton.Content = "COPIED!";
        }

        private void GUIs_fontStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if(!GUIs_fontStyleComboBox.SelectionBoxItem.ToString().Equals(_mainWindow.FontStyle)) {
                ReloadFontStyle(GUIs_fontStyleComboBox.SelectionBoxItem.ToString());
            }
        }


        //idk 
        public void ReloadFontStyle(string fontFamily) {
            this.FontFamily = new FontFamily(fontFamily);
        }

    }


    public class TdSettingsLogic {

        //init2
        private MainWindow _mainWindow;
        private Settings _settings;

        List<string> SettingsList = new List<string>();

        public TdSettingsLogic(MainWindow mainwin, Settings sett) {
            _mainWindow = mainwin;
            _settings = sett;
        }


        //load settings
        public void SyncAndLoad() {

            //load out of settings.txt
            if (SettingsList.Any()) { SettingsList.Clear(); }
            SettingsList = File.ReadAllLines(_mainWindow.SettingsFilePath).ToList();

            //FilePath
            _settings.GUIs_filePathTextBox.Text = SettingsList[0];

            //FontSize
            _settings.FontSize = double.Parse(SettingsList[1]);
            _settings.GUIs_fontSizeTextBox.Text = SettingsList[1];

            //ResetAllGUI
            if (bool.Parse(SettingsList[2])) { _settings.GUIs_resetAllAfterActionComboBox.SelectedIndex = 0; }
            else { _settings.GUIs_resetAllAfterActionComboBox.SelectedIndex = 1; }

            //SortCheckedTasks
            if (bool.Parse(SettingsList[3])) { _settings.GUIs_sortCheckedTasksLow.SelectedIndex = 0; }
            else { _settings.GUIs_sortCheckedTasksLow.SelectedIndex = 1; }

            //SortOutCheckedTasks
            if (bool.Parse(SettingsList[4])) { _settings.GUIs_sortOutCheckedTasks.SelectedIndex = 0; }
            else { _settings.GUIs_sortOutCheckedTasks.SelectedIndex = 1; }

            //OpenSettingsFileWithApp
            _settings.GUIs_openSettingsFileWithAppComboBox.SelectedIndex = 0;

            //SortOutCheckedTasks
            _settings.GUIs_sortOutCheckedTasksProfileNameTextBox.Text = SettingsList[5];

            //FontStyle 
            for (int i = 0; i < _settings.GUIs_fontStyleComboBox.Items.Count; i++) {
                if (SettingsList[7].Equals(_settings.GUIs_fontStyleComboBox.Items.GetItemAt(i).ToString())) {
                    _settings.GUIs_fontStyleComboBox.SelectedIndex = i;
                }
            }

            _settings.GUIs_saveTimerOnStop.SelectedIndex = bool.Parse(SettingsList[8]) ? 0 : 1; 
        }


        //save and return to MainWindow (who would`ve thought)
        public void SaveAndReturn() {

            _mainWindow.FilePathChanged = false;

            //FilePath
            if (IsRightFileTypeAndExists(_settings.GUIs_filePathTextBox.Text, ".db") && _settings.GUIs_filePathTextBox.Text != SettingsList[0]) {
                SettingsList[0] = _settings.GUIs_filePathTextBox.Text;
                _mainWindow.FilePath = SettingsList[0];
                _mainWindow.SQLValidFilePath = true;
                _mainWindow.FilePathChanged = true;
            }

            //FontSize
            if (double.Parse(_settings.GUIs_fontSizeTextBox.Text) > 20) { _settings.GUIs_fontSizeTextBox.Text = "20"; }
            else if (double.Parse(_settings.GUIs_fontSizeTextBox.Text) < 5) { _settings.GUIs_fontSizeTextBox.Text = "5"; }

            SettingsList[1] = _settings.GUIs_fontSizeTextBox.Text;
            _mainWindow.FontSizeDouble = double.Parse(SettingsList[1]);

            //ResetAllGUI
            SettingsList[2] = _settings.GUIs_resetAllAfterActionComboBox.SelectionBoxItem.ToString().ToLower();

            //SortCheckedTasks
            SettingsList[3] = _settings.GUIs_sortCheckedTasksLow.SelectionBoxItem.ToString().ToLower();

            //SortOutCheckedTasks 
            SettingsList[4] = _settings.GUIs_sortOutCheckedTasks.SelectionBoxItem.ToString().ToLower();

            if (!_settings.GUIs_sortOutCheckedTasksProfileNameTextBox.Text.Equals(SettingsList[5]) && !string.IsNullOrWhiteSpace(_settings.GUIs_sortOutCheckedTasksProfileNameTextBox.Text)) {
                SettingsList[5] = _settings.GUIs_sortOutCheckedTasksProfileNameTextBox.Text;
            }

            //FontStyle
            if (!_settings.GUIs_fontStyleComboBox.SelectionBoxItem.ToString().Equals(SettingsList[7])) {
                SettingsList[7] = _settings.GUIs_fontStyleComboBox.SelectionBoxItem.ToString();
            }

            if (!_settings.GUIs_saveTimerOnStop.SelectionBoxItem.ToString().Equals(SettingsList[8])) {
                SettingsList[8] = _settings.GUIs_saveTimerOnStop.SelectionBoxItem.ToString();
            }

            File.WriteAllLines(_mainWindow.SettingsFilePath, SettingsList);
        }


        private bool IsRightFileTypeAndExists(string file, string expectedExtension) {

            if (!File.Exists(file)) { return false; }

            string fileExtension = Path.GetExtension(file);

            return fileExtension.Equals(expectedExtension);
        }
    }
}


/*
0 = filePath 
1 = fontSize
2 = ResetAll(GUI)AfterChange
3 = SortCheckedTasksLow
4 = SortOutCheckedTasks
5 = SortOutCheckedTasksProfileName
6 = OldSortOutCheckedTasksProfileName
7 = FontStyle
*/