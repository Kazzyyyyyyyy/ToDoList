using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.IO;

using System.Data.SQLite;
using System.Runtime.InteropServices;
using System.Dynamic;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media;
using System.Threading;
using System.Windows.Threading;
using System.Timers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Media.Animation;
using System.Windows.Controls;

namespace ToDoList {

    //✔️

    public partial class MainWindow : Window {

        //init
        private TdMainWindowLogic _tdProgramm;
        private SQLiteConnection _connect;
        private Settings _settings;

        public string FilePath;
        public string SettingsFilePath;
        public string SettingsDirectory;
        public string LogFilePath;
        public bool FilePathChanged;
        public bool SortCheckedTasksLow;
        public bool SQLValidFilePath;
        public List<string> SettingsList = new List<string>();
        public string SQLDataSource;
        public bool SortOutCheckedTasks;
        public string FontStyle;
        public string SortedOutCheckedTasksProfileName, OldSortedOutCheckedTasksProfileName;
        public double FontSizeDouble;
        public bool SaveTimerOnStop;

        private DispatcherTimer _timer;


        public MainWindow() {

            InitializeComponent();

            _tdProgramm = new TdMainWindowLogic(this, _connect, _settings);

            _tdProgramm.StartupManager();

            _tdProgramm.SyncComboBoxHeight();
            _tdProgramm.SQL_connectToFile();

        }

        //events
        private void GUI_confirmButton_Click(object sender, RoutedEventArgs e) {

            if (!_tdProgramm.ProfileSelected || GUI_profileCommandComboBox.SelectedIndex != -1) {

                _tdProgramm.ProfileManager();
                _tdProgramm.CheckProfilesPossibleToLoad();
                _tdProgramm.ResetAndSyncAfterRun();

                return;
            }

            _tdProgramm.Main();

            if (GUI_cmdComboBox.SelectedIndex == 4 && SortOutCheckedTasks) {
                _tdProgramm.SyncTasks();
                _tdProgramm.SortCheckedTasksManager();
                _tdProgramm.SyncList();
            }

            _tdProgramm.SyncTasks();

            _tdProgramm.ResetAndSyncAfterRun();
        }

        private void GUI_settingsButton_Click(object sender, RoutedEventArgs e) {

            MainWindowGrid.Visibility = Visibility.Collapsed;
            MainFrame.NavigationService.Navigate(new Settings());
        }
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            
            if(Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.C) {
                if(GUI_taskList.SelectedIndex == -1) { return; }

                List<string> clip = _tdProgramm.GetTasksWithoutPrefix();

                Clipboard.SetText(clip[GUI_taskList.SelectedIndex]);
            }
        }

        private void GUI_timerButtons_Click(object sender, RoutedEventArgs e) {
            if (sender == GUI_timerStartButton) {
                _tdProgramm.StartTimer();
            }
            else if (sender == GUI_timerStopButton) {
                _tdProgramm.StopTimer();

                if(SaveTimerOnStop) {
                    _tdProgramm.SafeTimerTime();
                }
            }
            else if (sender == GUI_timerSafeButton) {
                _tdProgramm.SafeTimerTime();
            }
            else if(sender == GUI_timerSetButton) {
                _tdProgramm.SetTimer();
            }
            else if(sender == GUI_timerResetButton) {
                _tdProgramm.ResetTimer();
            }
        }

        //settings
        public void SyncSettingChanges() {

            SettingsList = File.ReadAllLines(SettingsFilePath).ToList();

            if (FilePath != "" && FilePathChanged) {
                _tdProgramm.SQL_connectToFile();
                _tdProgramm.ClearAndResetAllData();
            }

            if (FontSizeDouble != FontSize) {
                FontSize = FontSizeDouble;
                GUI_taskList.FontSize = FontSizeDouble + 5;
                _tdProgramm.SyncComboBoxHeight();
            }

            if (SortCheckedTasksLow != bool.Parse(SettingsList[3]) && _tdProgramm.ProfileSelected) {
                SortCheckedTasksLow = bool.Parse(SettingsList[3]);
                _tdProgramm.SyncTasks();
            }

            if(SortOutCheckedTasks != bool.Parse(SettingsList[4])) {
                SortOutCheckedTasks = bool.Parse(SettingsList[4]);

                _tdProgramm.SortCheckedTasksManager();

                if (_tdProgramm.ProfileSelected) {
                    _tdProgramm.SyncList();
                }
            }

            if (!SettingsList[5].Equals(SettingsList[6])) {
                SortedOutCheckedTasksProfileName = SettingsList[5];
                OldSortedOutCheckedTasksProfileName = SettingsList[6];

                _tdProgramm.ManageSOCTTableDifference();

                SettingsList[6] = SettingsList[5];

                File.WriteAllLines(SettingsFilePath, SettingsList);
            }

            if (!FontStyle.Equals(SettingsList[7])) {
                FontStyle = SettingsList[7];

                this.FontFamily = new FontFamily(FontStyle);
            }

            SaveTimerOnStop = bool.Parse(SettingsList[8]);
            if (SaveTimerOnStop) {
                GUI_timerSafeButton.Visibility = Visibility.Collapsed;
            }
            else {
                GUI_timerSafeButton.Visibility = Visibility.Visible;
            }

            _tdProgramm.CheckProfilesPossibleToLoad();
        }
    }

    //Code class
    public class TdMainWindowLogic {

        //init2
        string UserInput;
        int TaskAtIndex;
        enum Prioritys { Low = 0, Medium, High, Urgent, Default }

        private MainWindow _mainWindow;
        public SQLiteConnection _sqlConnect;

        public TdMainWindowLogic(MainWindow mainWindow, SQLiteConnection connect, Settings sett) {
            _mainWindow = mainWindow;
            _sqlConnect = connect;
        }

        //TIMER-FUNKTIONS
        DispatcherTimer TIMER;
        public void StartTimer() {

            if(SelectedProfileName == null) {
                OutPut("Can only start timer when table loaded");
                return;
            }

            TIMER = new DispatcherTimer();
            TIMER.Interval = TimeSpan.FromSeconds(1);

            TIMER.Tick += (sender, e) => UpdateTimer();

            TIMER.Start();
        }

        TimeSpan timeSpan;
        void UpdateTimer() {
            timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
            _mainWindow.GUI_timerOutPutTextBlock.Text = timeSpan.ToString(@"hh\:mm\:ss");
        }

        public void StopTimer() {
            TIMER.Stop();
        }

        public void ResetTimer() {
            timeSpan = TimeSpan.ParseExact("00:00:00", @"hh\:mm\:ss", null);
            _mainWindow.GUI_timerOutPutTextBlock.Text = "00:00:00"; 
        }

        public void SetTimer() {
            if (SelectedProfileName == null) {
                OutPut("Can only set timer when table laoded");
                return;
            }

            if (_mainWindow.GUI_taskInpText.Text.Length == 0) {
                OutPut("enter a valid time in valid");
                return; 
            }

            _mainWindow.GUI_timerOutPutTextBlock.Text = _mainWindow.GUI_taskInpText.Text;
            timeSpan = TimeSpan.ParseExact(_mainWindow.GUI_timerOutPutTextBlock.Text, @"hh\:mm\:ss", null);
            OutPut("time set!");
        }

        public void SafeTimerTime() {
            try {
                if (_sqlConnect == null || _mainWindow.GUI_timerOutPutTextBlock.Text.Length == 0) { 
                    OutPut("Connect to a SQLite - DB first / start timer"); 
                    return; 
                }

                using (var cmd = new SQLiteCommand(SQL_safeTimerTime, _sqlConnect)) {
                    cmd.Parameters.AddWithValue("@table", SelectedProfileName);
                    cmd.Parameters.AddWithValue("@time", _mainWindow.GUI_timerOutPutTextBlock.Text);
                    cmd.ExecuteNonQuery();
                }

                OutPut("time safed");
            }
            catch (Exception ex) { 
                Programmlog(ex.ToString());
            }
        }

        void LoadTimerOnTableLoad() {


            using (var cmd = new SQLiteCommand(SQL_createTimerTable, _sqlConnect)) {
                cmd.ExecuteNonQuery();
            }

            using (var cmd = new SQLiteCommand(SQL_getAllTasksString(SQL_timerSafeTableName), _sqlConnect)) {
                using(var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        if(reader.GetString(0) == SelectedProfileName) {
                            timeSpan = TimeSpan.ParseExact(reader.GetString(1), @"hh\:mm\:ss", null);
                            _mainWindow.GUI_timerOutPutTextBlock.Text = reader.GetString(1);
                            return; 
                        }
                    }

                    timeSpan = TimeSpan.ParseExact("00:00:00", @"hh\:mm\:ss", null);
                    _mainWindow.GUI_timerOutPutTextBlock.Text = "00:00:00";
                }
            }
        }

        public void Programmlog(string log) {
            File.WriteAllText(_mainWindow.LogFilePath, log); 
        }

        public void StartupManager() {

            _mainWindow.SettingsDirectory = $@"C:\Users\{Environment.UserName}\AppData\Local\ToDoListSQL";
            _mainWindow.SettingsFilePath = $@"C:\Users\{Environment.UserName}\AppData\Local\ToDoListSQL\SettingsSave.txt";
            _mainWindow.LogFilePath = $@"C:\Users\{Environment.UserName}\AppData\Local\ToDoListSQL\log.txt";
            _mainWindow.FilePath = $@"C:\Users\{Environment.UserName}\AppData\Local\ToDoListSQL\save.db";

            if (!Directory.Exists($@"C:\Users\{Environment.UserName}\AppData\Local\ToDoListSQL")) {
                Directory.CreateDirectory(_mainWindow.SettingsDirectory);
            }

            //SettingsFile
            if (!File.Exists(_mainWindow.FilePath)) {
                File.Create(_mainWindow.FilePath).Dispose();
            }

            if (!File.Exists(_mainWindow.SettingsFilePath)) {
                File.Create(_mainWindow.SettingsFilePath).Dispose();

                List<string> SetSettings = new List<string> { _mainWindow.FilePath, "15", "true", "true", "true", "FinishedTasks", "FinishedTasks", "Arial", "true" };
                File.WriteAllLines(_mainWindow.SettingsFilePath, SetSettings);
            }

            if (!File.Exists(_mainWindow.LogFilePath)) {
                File.Create(_mainWindow.LogFilePath).Dispose();
            }


            _mainWindow.SettingsList = File.ReadAllLines(_mainWindow.SettingsFilePath).ToList();

            //FilePath
            _mainWindow.FilePath = _mainWindow.SettingsList[0];

            if (!File.Exists(_mainWindow.FilePath)) { _mainWindow.SQLValidFilePath = false; }
            else { _mainWindow.SQLValidFilePath = true; }

            //FontSize
            _mainWindow.FontSizeDouble = double.Parse(_mainWindow.SettingsList[1].ToLower());
            _mainWindow.FontSize = _mainWindow.FontSizeDouble;
            _mainWindow.GUI_taskList.FontSize = _mainWindow.FontSizeDouble + 5;

            //SortCheckedTasks
            _mainWindow.SortCheckedTasksLow = bool.Parse(_mainWindow.SettingsList[3]);

            _mainWindow.SortOutCheckedTasks = bool.Parse(_mainWindow.SettingsList[4]);

            _mainWindow.SortedOutCheckedTasksProfileName = _mainWindow.SettingsList[5];
            _mainWindow.OldSortedOutCheckedTasksProfileName = _mainWindow.SettingsList[6];

            //FontStyle
            _mainWindow.FontStyle = _mainWindow.SettingsList[7];

            //timer
            _mainWindow.SaveTimerOnStop = bool.Parse(_mainWindow.SettingsList[8]);
            _mainWindow.GUI_timerSafeButton.Visibility = _mainWindow.SaveTimerOnStop ? Visibility.Collapsed : Visibility.Visible;

        }


        //SQLite
        private string SQL_getAllProfileString = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
        private string SQL_getAllTasksString(string tname) { return $"SELECT * FROM {tname}"; }
        private string SQL_addProfileString(string tname) {
            return $@"CREATE TABLE IF NOT EXISTS {tname}(
                     ID INTEGER PRIMARY KEY AUTOINCREMENT,
                     Tasks TEXT NOT NULL)";
        }
        static string SQL_timerSafeTableName = "TimerSafeStateTable"; 
        static string SQL_createTimerTable = 
           $@"CREATE TABLE IF NOT EXISTS {SQL_timerSafeTableName}(
                     tableName TEXT PRIMARY KEY,
                     time TEXT NOT NULL)";

        static string SQL_safeTimerTime = $"INSERT OR REPLACE INTO {SQL_timerSafeTableName} (tableName, time) VALUES (@table, @time)";
        static string SQL_updateTimer(string fromTableName, string updateTime) { 
            return $"UPDATE {SQL_timerSafeTableName} SET time  = '{updateTime}' WHERE tableName = '{fromTableName}'";
        }

        public string SQL_sortOutCheckTasksString() { return $@"CREATE TABLE IF NOT EXISTS {_mainWindow.SortedOutCheckedTasksProfileName}(
                                                       ID INTEGER PRIMARY KEY AUTOINCREMENT,
                                                       Tasks TEXT NOT NULL, 
                                                       FromTable TEXT NOT NULL)"; }
        private string SQL_addTaskString(string table) { return $"INSERT INTO {table} (Tasks) VALUES (@task)"; }
        private string SQL_removeProfileString(string name) { return $"DROP TABLE {name}"; }
        private string SQL_removeAllFromTable(string name) { return $"DELETE FROM {name}"; }
        private string SQL_addCheckedTaskString() { return $"INSERT INTO {_mainWindow.SortedOutCheckedTasksProfileName} (Tasks, FromTable) VALUES (@task, @fromTable)"; } 
        private string SQL_renameTable(string tableName, string newName) { return $"ALTER TABLE {tableName} RENAME TO {newName}"; }


        public void SQL_connectToFile() {

            if (_sqlConnect != null) { _sqlConnect.Close(); }

            if (_mainWindow.SQLValidFilePath) {
                try {
                    _sqlConnect = new SQLiteConnection($@"Data Source={_mainWindow.FilePath}");
                    _sqlConnect.Open();
                    _mainWindow.SQLDataSource = _sqlConnect.DataSource;
                    OutPut("SQL connected");
                }
                catch (Exception e) { OutPut($"Error (connectToFile) {e}"); }

                CheckProfilesPossibleToLoad(); 
                return;
            }

            OutPut("Enter a valid DataBase FilePath in Settings");

        }


        //get and return functions
        public void OutPut(string outPut) { _mainWindow.GUI_outputTextBlock.Text = outPut; }
        private string ReturnChangeMessage(int prio, string input) { return $"Task changed to '{input}' with priority '{(Prioritys)prio}'"; }

        private int GetIntByPrioritys(string unsortedTasks) {

            if (unsortedTasks.Contains(CheckMarkString) && bool.Parse(_mainWindow.SettingsList[3])) { return 6; }

            else if (unsortedTasks.StartsWith($"{(Prioritys)3}: ")) return 1;
            else if (unsortedTasks.StartsWith($"{(Prioritys)2}: ")) return 2;
            else if (unsortedTasks.StartsWith($"{(Prioritys)1}: ")) return 3;
            else if (unsortedTasks.StartsWith($"{(Prioritys)0}: ")) return 4;
            else if (unsortedTasks.StartsWith($"{(Prioritys)4}: ")) return 5;

            return 100;
        }

        private List<string> GetTasksWithoutSuffix() {

            List<string> taskWithoutSuffix = new List<string>();

            foreach (string read in Tasks) {
                if (read.Contains(CheckMarkString)) {
                    taskWithoutSuffix.Add(read.Replace(CheckMarkString, ""));
                }
                else {
                    taskWithoutSuffix.Add(read);
                }
            }

            return taskWithoutSuffix;
        }

        public List<string> GetTasksWithoutPrefix() {

            List<string> taskWithoutSuffix = GetTasksWithoutSuffix();
            List<string> taskWithoutPrefix = new List<string>();

            foreach (string read in taskWithoutSuffix) {
                if (read.StartsWith($"{(Prioritys)0}: ")) { taskWithoutPrefix.Add(read.Replace($"{(Prioritys)0}: ", "")); }
                else if (read.StartsWith($"{(Prioritys)1}: ")) { taskWithoutPrefix.Add(read.Replace($"{(Prioritys)1}: ", "")); }
                else if (read.StartsWith($"{(Prioritys)2}: ")) { taskWithoutPrefix.Add(read.Replace($"{(Prioritys)2}: ", "")); }
                else if (read.StartsWith($"{(Prioritys)3}: ")) { taskWithoutPrefix.Add(read.Replace($"{(Prioritys)3}: ", "")); }
                else if (read.StartsWith($"{(Prioritys)4}: ")) { taskWithoutPrefix.Add(read.Replace($"{(Prioritys)4}: ", "")); }
            }
            return taskWithoutPrefix;
        }

        private int GetPrefixIntWithoutTask(string checkTask) {

            if (checkTask.StartsWith($"{(Prioritys)0}: ")) { return 0; }
            else if (checkTask.StartsWith($"{(Prioritys)1}: ")) { return 1; }
            else if (checkTask.StartsWith($"{(Prioritys)2}: ")) { return 2; }
            else if (checkTask.StartsWith($"{(Prioritys)3}: ")) { return 3; }
            else if (checkTask.StartsWith($"{(Prioritys)4}: ")) { return 4; }

            return -1;
        }

        private int TaskActionPerClickOrChat() {

            if (!string.IsNullOrWhiteSpace(UserInput) && SearchForTasks()) { return 1; }
            else if (string.IsNullOrWhiteSpace(UserInput) && _mainWindow.GUI_taskList.SelectedIndex > -1 && _mainWindow.GUI_prioComboBox.SelectedIndex > -1) { return 3; }
            else if (_mainWindow.GUI_taskList.SelectedIndex > -1) { return 2; }

            return 0;
        }

        private bool SearchForTasks() {

            List<string> taskWithoutPrefixAndCheck = GetTasksWithoutPrefix();

            for (int i = 0; i < taskWithoutPrefixAndCheck.Count; i++) {
                if (taskWithoutPrefixAndCheck[i].Equals(UserInput, StringComparison.OrdinalIgnoreCase)) {
                    TaskAtIndex = i;
                    return true;
                }
            }
            return false;
        }

        private string SearchForTasksString(int taskIndex) {

            List<string> taskWtPrefix = GetTasksWithoutPrefix();
            string task = taskWtPrefix[taskIndex];

            for (int i = 0; i < Tasks.Count; i++) {
                if (taskWtPrefix[i].Equals(task, StringComparison.OrdinalIgnoreCase)) {
                    return taskWtPrefix[i];
                }
            }
            return null;
        }


        //GUI
        public void SyncList() {

            _mainWindow.GUI_taskList.Items.Clear();

            foreach (string read in Tasks) {
                _mainWindow.GUI_taskList.Items.Add(read);
            }
        }

        public void SyncComboBoxHeight() {

            if (_mainWindow.FontSizeDouble > 15) {
                _mainWindow.GUI_cmdComboBox.Height = 30;
                _mainWindow.GUI_prioComboBox.Height = 30;
                _mainWindow.GUI_profileCommandComboBox.Height = 30;
                _mainWindow.GUI_profileSelectComboBox.Height = 30;

                return;
            }

            _mainWindow.GUI_cmdComboBox.Height = 22.5;
            _mainWindow.GUI_prioComboBox.Height = 22.5;
            _mainWindow.GUI_profileCommandComboBox.Height = 22.5;
            _mainWindow.GUI_profileSelectComboBox.Height = 22.5;
        }

        public void ClearAndResetAllData() {

            ProfileSelected = false;
            SelectedProfileName = null;
            _mainWindow.GUI_profileSelectedTextBlock.Text = "No profile loaded";

            _mainWindow.GUI_taskList.Items.Clear();

            Tasks.Clear();
        }

        public void ResetAndSyncAfterRun() {

            if (bool.Parse(_mainWindow.SettingsList[2].ToLower())) {
                _mainWindow.GUI_cmdComboBox.SelectedIndex = -1;
                _mainWindow.GUI_prioComboBox.SelectedIndex = -1;
                _mainWindow.GUI_profileCommandComboBox.SelectedIndex = -1;
                _mainWindow.GUI_taskList.SelectedIndex = -1;

                _mainWindow.GUI_taskInpText.Text = null;
            }
        }


        //profile management
        string SelectedProfileName = null;
        public bool ProfileSelected = false;

        List<string> Profiles = new List<string>();
        List<string> Tasks = new List<string>();
        List<string> OneStateBehindTasks = new List<string>();

        public void ProfileManager() {

            UserInput = _mainWindow.GUI_taskInpText.Text;
            switch (_mainWindow.GUI_profileCommandComboBox.SelectedIndex) {
                case 0:
                    AddProfile();
                    break;

                case 1:
                    RemoveProfile();
                    break;

                case 2:
                    SelectAndloadProfile();
                    break;
            }
        }

        private void AddProfile() {

            if (string.IsNullOrWhiteSpace(UserInput) || UserInput.Contains(' ') || UserInput.Contains('-')) { OutPut("Enter a valid name"); return; }
            

            if (FindProfile(UserInput)) { OutPut($"Profile '{UserInput}' already added"); return; }

            using (var cmd = new SQLiteCommand(SQL_addProfileString(UserInput), _sqlConnect)) {
                cmd.ExecuteNonQuery(); OutPut($"Profile '{UserInput}' added");
            }
        }

        private void RemoveProfile() {

           if(_mainWindow.GUI_profileSelectComboBox.SelectedIndex == -1) { OutPut("Select a profile"); return; }

            using (var cmd = new SQLiteCommand(SQL_removeProfileString(_mainWindow.GUI_profileSelectComboBox.SelectedItem.ToString()), _sqlConnect)) { 
                cmd.ExecuteNonQuery();
                OutPut($"Profile '{_mainWindow.GUI_profileSelectComboBox.SelectedItem.ToString()}' removed"); 
            }

            if(SelectedProfileName != _mainWindow.GUI_profileSelectComboBox.SelectedItem.ToString()) { return; }

            SelectedProfileName = null;
            ProfileSelected = false;
            _mainWindow.GUI_profileSelectedTextBlock.Text = "No profile loaded";

            _mainWindow.GUI_taskList.Items.Clear();
        }

        private void SelectAndloadProfile() {

            if(_mainWindow.GUI_profileSelectComboBox.SelectedIndex == -1) { OutPut("Select a profile"); return; }

            if (SelectedProfileName == _mainWindow.GUI_profileSelectComboBox.SelectedItem.ToString()) { OutPut($"Profile '{SelectedProfileName}' already loaded"); return; }

            SelectedProfileName = _mainWindow.GUI_profileSelectComboBox.SelectedItem.ToString();

            using (var cmd = new SQLiteCommand(SQL_getAllTasksString(SelectedProfileName), _sqlConnect)) {
                using (var reader = cmd.ExecuteReader()) {
                    Tasks.Clear();
                    while (reader.Read()) {
                        Tasks.Add(reader["Tasks"].ToString());
                    }
                } 
            }

            ProfileSelected = true;
            SyncTasks();
            SaveLastCicleTasks();
            LoadTimerOnTableLoad(); 
            _mainWindow.GUI_profileSelectedTextBlock.Text = SelectedProfileName;
            OutPut($"Profile '{SelectedProfileName}' loaded");
        }

        private bool FindProfile(string inp) {

            foreach(string read in Profiles) { 
                if(read.Equals(inp, StringComparison.OrdinalIgnoreCase)) { return true; } 
            }
            return false;
        }

        public void CheckProfilesPossibleToLoad() {

            Profiles.Clear();
            _mainWindow.GUI_profileSelectComboBox.Items.Clear();

            using (var cmd = new SQLiteCommand(SQL_getAllProfileString, _sqlConnect)) {
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        Profiles.Add(reader["name"].ToString());
                    }
                }
            }

            foreach (string read in Profiles) {
                if(read.Equals(_mainWindow.SortedOutCheckedTasksProfileName) || read.Equals(SQL_timerSafeTableName)) { continue; }

                _mainWindow.GUI_profileSelectComboBox.Items.Add(read);
            }

            if(Profiles.Contains(_mainWindow.SortedOutCheckedTasksProfileName) && _mainWindow.SortOutCheckedTasks) {
                _mainWindow.GUI_profileSelectComboBox.Items.Add(_mainWindow.SortedOutCheckedTasksProfileName);
            }
        }

        private void SortInCheckedTasks() {

            using (var cmd = new SQLiteCommand(SQL_getAllTasksString(_mainWindow.SortedOutCheckedTasksProfileName), _sqlConnect)) {
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {

                        using (var cmd2 = new SQLiteCommand(SQL_addTaskString(reader["FromTable"].ToString()), _sqlConnect)) {
                            cmd2.Parameters.Clear();
                            cmd2.Parameters.AddWithValue("@task", reader["Tasks"].ToString());
                            cmd2.ExecuteNonQuery();
                            cmd2.Dispose();
                        }

                        if (reader["FromTable"].ToString().Equals(SelectedProfileName)) {
                            Tasks.Add(reader["Tasks"].ToString());
                        }
                    }
                }
                cmd.Dispose();

                if(SelectedProfileName != null && SelectedProfileName.Equals(_mainWindow.SortedOutCheckedTasksProfileName)) {
                    ClearAndResetAllData();
                }
            }

            using (var cmd = new SQLiteCommand(SQL_removeAllFromTable(_mainWindow.SortedOutCheckedTasksProfileName), _sqlConnect)) { 
                cmd.ExecuteNonQuery();

                if (SelectedProfileName == _mainWindow.SortedOutCheckedTasksProfileName) {
                    Tasks.Clear();
                }
            }
        }

        private void ReloadTableAfterSOCT(string table, List<string> reloadTableList) {

            using (var cmd = new SQLiteCommand(SQL_removeAllFromTable(table), _sqlConnect)) { cmd.ExecuteNonQuery(); cmd.Dispose(); }

            using (var transact = _sqlConnect.BeginTransaction()) {
                using (var cmd = new SQLiteCommand(SQL_addTaskString(table), _sqlConnect)) {
                    foreach (string task in reloadTableList) {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@task", task);
                        cmd.ExecuteNonQuery();
                    }
                    cmd.Dispose();
                }
                transact.Commit();
                transact.Dispose();
                reloadTableList.Clear();
            }
        }

        private void CheckSOCTTable(List<string> SOCTTasks, List<string> SOCTTable) {

            using (var cmd = new SQLiteCommand(SQL_getAllTasksString(_mainWindow.SortedOutCheckedTasksProfileName), _sqlConnect)) {
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        if (!reader["Tasks"].ToString().Any()) { return; }

                        SOCTTasks.Add(reader["Tasks"].ToString());
                        SOCTTable.Add(reader["FromTable"].ToString());

                    }
                    reader.Close();
                }
                cmd.Dispose();
            }
        }

        private void AddTasksToSOCTLists(string table, List<string> reloadTableList, List<string> SOCTTasks, List<string> SOCTTable) {

            using (var cmd = new SQLiteCommand(SQL_getAllTasksString(table), _sqlConnect)) {
                using (var reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        if (table.Equals(_mainWindow.SortedOutCheckedTasksProfileName)) { return; }

                        string readerTasks = reader["Tasks"].ToString();
                        if (readerTasks.EndsWith(CheckMarkString)) {
                            SOCTTasks.Add(readerTasks);
                            SOCTTable.Add(table);

                            if (SelectedProfileName != null && SelectedProfileName.Equals(_mainWindow.SortedOutCheckedTasksProfileName)) {
                                Tasks.Add(readerTasks);
                            }

                            if (SelectedProfileName != null && table.Equals(SelectedProfileName)) {
                                Tasks.Remove(readerTasks);
                            }
                        }
                        else {
                            reloadTableList.Add(readerTasks);
                        }
                    }
                }
                cmd.Dispose();
            }
        }

        private void AddTasksToSOCTTable(List<string> SOCTTasks, List<string> SOCTTable) {
            using (var transact = _sqlConnect.BeginTransaction()) {
                using(var cmd = new SQLiteCommand(SQL_addCheckedTaskString(), _sqlConnect)) {

                    for (int i = 0; i < SOCTTasks.Count; i++) {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@task", SOCTTasks[i]);
                        cmd.Parameters.AddWithValue("@fromTable", SOCTTable[i]);
                        cmd.ExecuteNonQuery();
                    }
                    cmd.Dispose();
                }
                transact.Commit();
                transact.Dispose();
            }
        }

        private void SortCheckedTasks() {

            var (reloadTableList, SOCTTasks, SOCTTable) = (new List<string>(), new List<string>(), new List<string>());

            CheckSOCTTable(SOCTTasks, SOCTTable);

            foreach (string table in Profiles) {

                AddTasksToSOCTLists(table, reloadTableList, SOCTTasks, SOCTTable);
                ReloadTableAfterSOCT(table, reloadTableList);
            }

            if (SOCTTasks.Any() && SOCTTable.Any()) {
                AddTasksToSOCTTable(SOCTTasks, SOCTTable);
            }
        }

        public void SortCheckedTasksManager() {
            
            if(!Profiles.Contains(_mainWindow.SortedOutCheckedTasksProfileName)) {
                using (var cmd = new SQLiteCommand(SQL_sortOutCheckTasksString(), _sqlConnect)) {
                    cmd.ExecuteNonQuery();
                }
            }

            if(_mainWindow.SortOutCheckedTasks) {
                SortCheckedTasks();
                //OutPut("Checked tasks sorted out");
                return; 
            }

            SortInCheckedTasks();
            //OutPut("Checked tasks sorted in");
        }

        private void RenameSOCTTable() {
            using(var cmd = new SQLiteCommand(SQL_renameTable(_mainWindow.OldSortedOutCheckedTasksProfileName, _mainWindow.SortedOutCheckedTasksProfileName), _sqlConnect)) {
                cmd.ExecuteNonQuery();
                cmd.Dispose();  
            }
        }

        public void ManageSOCTTableDifference() {
            RenameSOCTTable();
        }


        //Task management
        private void OrderTasks() {
            Tasks = Tasks.OrderBy(unsortedTasks => GetIntByPrioritys(unsortedTasks)).ToList();
        }

        public void SyncTasks() {

            OrderTasks();

            if (!SelectedProfileName.Equals(_mainWindow.SortedOutCheckedTasksProfileName)) {
                using (var cmd = new SQLiteCommand(SQL_removeAllFromTable(SelectedProfileName), _sqlConnect)) { cmd.ExecuteNonQuery(); cmd.Dispose(); }
            }

            using (var transact = _sqlConnect.BeginTransaction()) {
                try {
                    using (var cmd = new SQLiteCommand(SQL_addTaskString(SelectedProfileName), _sqlConnect)) {
                        foreach (string task in Tasks) {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@task", task);
                            cmd.ExecuteNonQuery();
                        }
                        cmd.Dispose();
                    }
                    transact.Commit();
                    transact.Dispose();
                }
                catch (Exception ex) { OutPut($"Error (SyncTasks) {ex}"); }
            }

            _mainWindow.GUI_taskList.Items.Clear();

            if (!Tasks.Any()) { return; }

            foreach (string read in Tasks) {
                _mainWindow.GUI_taskList.Items.Add(read);
            }
        }


        //cmd functions
        private string ReturnTaskAddFormular(string input) { return $"{(Prioritys)_mainWindow.GUI_prioComboBox.SelectedIndex}: {input}"; }
        private string ReturnTaskAddFormularWithOtherPrio(int prio, string input) { return $"{(Prioritys)prio}: {input}"; }

        private void AddTasks() {

            if (string.IsNullOrWhiteSpace(UserInput)) { OutPut("Enter a task to add"); return; }

            if (SearchForTasks()) { OutPut($"Task '{UserInput}' already added"); return; }

            SaveLastCicleTasks();

            if (_mainWindow.GUI_prioComboBox.SelectedIndex == -1) {
                Tasks.Add($"{(Prioritys)4}: {UserInput}");

                OutPut($"Task '{UserInput}' with priority '{(Prioritys)4}' added");
                return; 
            }

            Tasks.Add(ReturnTaskAddFormular(UserInput));

            OutPut($"Task '{UserInput}' with priority '{(Prioritys)_mainWindow.GUI_prioComboBox.SelectedIndex}' added");
        }

        private int GetTaskRemoveIndex() {

            if (UserInput.Any() && UserInput == "(all)" && Tasks.Count > 0) { return -2; }

            switch (TaskActionPerClickOrChat()) {
                case 1: return TaskAtIndex;
                case 2: return _mainWindow.GUI_taskList.SelectedIndex;
                default: return -1;
            }
        }

        private void RemoveTasks() {

            SaveLastCicleTasks();
            int index = GetTaskRemoveIndex();

            if (index == -1) { return; }
            else if (index == -2) {
                index = Tasks.Count;

                Tasks.Clear();

                OutPut($"All {index} files removed");
                return;
            }

            string removedTask = Tasks[index];

            Tasks.RemoveAt(index);

            OutPut($"Task '{removedTask}' removed");
        }

        private void EditTasks() {

            SaveLastCicleTasks();

            switch (TaskActionPerClickOrChat()) {
                case 1:
                case 2:
                    if(string.IsNullOrWhiteSpace(UserInput)) { OutPut("Enter a task to change to"); return; }

                    if(SearchForTasks()) { OutPut("Cant change task to already added one"); return; }

                    Tasks.RemoveAt(_mainWindow.GUI_taskList.SelectedIndex);

                    if (_mainWindow.GUI_prioComboBox.SelectedIndex == -1) {
                        Tasks.Insert(_mainWindow.GUI_taskList.SelectedIndex, ReturnTaskAddFormularWithOtherPrio(GetPrefixIntWithoutTask(_mainWindow.GUI_taskList.SelectedItem.ToString()), UserInput));
                        OutPut(ReturnChangeMessage(GetPrefixIntWithoutTask(_mainWindow.GUI_taskList.SelectedItem.ToString()), UserInput));
                    }
                    else {
                        Tasks.Insert(_mainWindow.GUI_taskList.SelectedIndex, ReturnTaskAddFormular(UserInput));
                        OutPut(ReturnChangeMessage(_mainWindow.GUI_taskList.SelectedIndex, UserInput));
                    }

                    break;

                case 3:
                    string task = SearchForTasksString(_mainWindow.GUI_taskList.SelectedIndex);

                    Tasks.RemoveAt(_mainWindow.GUI_taskList.SelectedIndex);
                    Tasks.Insert(_mainWindow.GUI_taskList.SelectedIndex, ReturnTaskAddFormular(task));

                    OutPut(ReturnChangeMessage(_mainWindow.GUI_taskList.SelectedIndex, task));
                    break;

                default: OutPut("Select a task / Select Priority / Enter a task to change to"); break;
            }
        }

        private void SaveLastCicleTasks() { OneStateBehindTasks = Tasks.ToList(); }
        private bool CheckIfUndoPossible() { return !Tasks.SequenceEqual(OneStateBehindTasks); }

        private void UndoChangesInTasks() {

            if (!CheckIfUndoPossible()) { OutPut("No changes to undo found"); return; }

            Tasks = OneStateBehindTasks;

            OutPut("Change undone");
        }


        private List<string> AddCheckMarkToTask() {

            List<string> taskwtcheck = new List<string>();

            foreach (string read in Tasks) {
                if (read.Equals(_mainWindow.GUI_taskList.SelectedItem.ToString())) {
                    taskwtcheck.Add(read + CheckMarkString);
                }
                else {
                    taskwtcheck.Add(read);
                }
            }

            return taskwtcheck; 
        }

        private List<string> RemoveCheckMarktFromTask() {

            List<string> taskwtcheck = new List<string>();

            foreach(string read in Tasks) {
                if (read.Equals(_mainWindow.GUI_taskList.SelectedItem.ToString())) {
                    taskwtcheck.Add(read.Replace(CheckMarkString, ""));
                }
                else {
                    taskwtcheck.Add(read);
                }
            }

            return taskwtcheck;
        }

        private static readonly string CheckMarkString = " ✔️";
        private void CheckMarkTasks() {

            SaveLastCicleTasks();
            if(_mainWindow.GUI_taskList.SelectedIndex == -1) { OutPut("Select a task to check"); return; }

            if(!Tasks.ElementAt(_mainWindow.GUI_taskList.SelectedIndex).EndsWith(CheckMarkString)) {
                Tasks = AddCheckMarkToTask();
                OutPut($"Task '{_mainWindow.GUI_taskList.SelectedItem.ToString()}' checkmarkt");
                return;
            }

            Tasks = RemoveCheckMarktFromTask();
            OutPut($"Task '{_mainWindow.GUI_taskList.SelectedItem.ToString()}' un-checked");

        }


        //main
        public void Main() {

            UserInput = _mainWindow.GUI_taskInpText.Text;

            switch (_mainWindow.GUI_cmdComboBox.SelectedIndex) {
                case 0:
                    if (SelectedProfileName == _mainWindow.SortedOutCheckedTasksProfileName) { return; }

                    AddTasks();
                    break;

                case 1:
                    RemoveTasks();
                    break;

                case 2:
                    if (SelectedProfileName == _mainWindow.SortedOutCheckedTasksProfileName) { return; }

                    EditTasks();
                    break;

                case 3:
                    if (SelectedProfileName == _mainWindow.SortedOutCheckedTasksProfileName) { return; }

                    UndoChangesInTasks();
                    break;

                case 4:
                    if (SelectedProfileName == _mainWindow.SortedOutCheckedTasksProfileName) { return; }

                    CheckMarkTasks();
                    break;

            }
        }
    }
}