using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OutpostBBSDescUpdate
{
    public partial class Form1 : Form
    {
        string m_sUserDataPath;
        string m_UserDataDirectory;
        string m_OutpostDataDirectory;
        BBSDescriptionData m_BBSDescriptionData;
        string[] m_Frequencies;
        string[] m_Secondaries;

        private class ComboBoxBBSNameItem
        {
            string _BBSName;
            int _BBSNameIndex;

            public ComboBoxBBSNameItem(string BBSName, int BBSNameIndex)
            {
                _BBSName = BBSName;
                _BBSNameIndex = BBSNameIndex;
            }

            public string BBSName
            {
                get { return _BBSName; }
            }

            public int BBSNameIndex
            {
                get { return _BBSNameIndex; }
            }

            public override string ToString()
            {
                return _BBSName;
            }
        }

        public Form1()
        {
            InitializeComponent();

            m_sUserDataPath = Application.UserAppDataPath;
            int index = m_sUserDataPath.LastIndexOf("Roaming");
            index = m_sUserDataPath.IndexOf('\\', index);
            m_OutpostDataDirectory = m_sUserDataPath.Substring(0, index + 1) + @"SCCo Packet\";
            // Check if directory exists
            if (!Directory.Exists(m_OutpostDataDirectory))
            {
                MessageBox.Show(string.Format("Outpost Data Directory can not be found. \n{0}\n Outpost may not be installed.", m_OutpostDataDirectory), "Modify Outpost BBS Description", MessageBoxButtons.OK);
            }

            this.textBoxOutpostDataPath.Text = m_OutpostDataDirectory;

            index = m_sUserDataPath.IndexOf("OutpostBBSDescUpdate");
            index = m_sUserDataPath.IndexOf('\\', index);
            m_UserDataDirectory = m_sUserDataPath.Substring(0, index + 1);
            m_sUserDataPath = m_UserDataDirectory + @"BBSData.xml";

            string BBSDirectory = m_OutpostDataDirectory + @"bbs.d";
            var bbsFiles = Directory.EnumerateFiles(BBSDirectory, "*.bbs", SearchOption.AllDirectories);

            // Initialize data some structures
            m_Frequencies = new string[bbsFiles.Count()];
            for (int i = 0; i < m_Frequencies.Length; i++ )
            {
                m_Frequencies[i] = "";
            }
            m_Secondaries = new string[bbsFiles.Count()];
            for (int i = 0; i < m_Secondaries.Length; i++)
            {
                m_Secondaries[i] = "";
            }
            string[] bbsNames = new string[bbsFiles.Count()];

            // Initialize BBS Name ComboBox with namas
            int BBSNameIndex = 0;
            foreach (var file in bbsFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileName = fileInfo.Name;
                index = fileName.LastIndexOf('.');
                bbsNames[BBSNameIndex] = fileName.Substring(0, index);
                comboBoxBBSName.Items.Add(new ComboBoxBBSNameItem(bbsNames[BBSNameIndex], BBSNameIndex));
                BBSNameIndex++;
            }

            FindFrequencies();

            string sError = "";
            BBSNameIndex = 0;
            m_BBSDescriptionData = new BBSDescriptionData();
            if (File.Exists(m_sUserDataPath))
            {
                BBSDescriptionData.ReadBBSDescriptionDataFromFile(m_sUserDataPath, ref m_BBSDescriptionData, out sError);
                //m_BBSDescriptionData.ReadData(m_sUserDataPath);
            }
            else
            {
                // Create first time data
                BBSDescriptionDataTacticalCallSign[] tacticalCallSigns = new BBSDescriptionDataTacticalCallSign[bbsFiles.Count()];
                m_BBSDescriptionData.TacticalCallSign = tacticalCallSigns;
                foreach (string file in bbsFiles)
                {
                    string callSign;
                    string BBSDescription = ReadBBSFileData(bbsNames[BBSNameIndex], out callSign);

                    BBSDescriptionDataTacticalCallSign tacticalCallSign = new BBSDescriptionDataTacticalCallSign();
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex] = tacticalCallSign;
                    BBSDescriptionDataTacticalCallSignOriginalDescription originalDescription = new BBSDescriptionDataTacticalCallSignOriginalDescription();
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].OriginalDescription = originalDescription;
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].OriginalDescription.description = BBSDescription;
                    BBSDescriptionDataTacticalCallSignNewDescription newDescription = new BBSDescriptionDataTacticalCallSignNewDescription();
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].NewDescription = newDescription;
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].NewDescription.description = BBSDescription;
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].primary = callSign;
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].secondary = m_Secondaries[BBSNameIndex];
                    m_BBSDescriptionData.TacticalCallSign[BBSNameIndex].frequencies = m_Frequencies[BBSNameIndex];

                    BBSNameIndex++;
                }
                m_BBSDescriptionData.WriteBBSDescriptionDataToFile(m_sUserDataPath, out sError);

                //m_BBSDescriptionData.SaveData(m_sUserDataPath);
            }
            if (m_BBSDescriptionData.TacticalCallSign.Count() != comboBoxBBSName.Items.Count)
            {
                MessageBox.Show("New tactical callsign(s) added.", "BBS Description Update", MessageBoxButtons.OK);
            }
            comboBoxBBSName.SelectedIndex = 0;
        }

        private void comboBoxBBSName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = comboBoxBBSName.SelectedItem as string;
            //string description = ReadBBSFileData(text);
            // read from temporary data
            var item = comboBoxBBSName.SelectedItem as ComboBoxBBSNameItem;
            textBoxDescription.Text = m_BBSDescriptionData.TacticalCallSign[item.BBSNameIndex].NewDescription.description;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string BBSName = comboBoxBBSName.SelectedItem as string;
            WriteBBSDescription(BBSName);
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonPath_Click(object sender, EventArgs e)
        {
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                m_OutpostDataDirectory = openFileDialog1.FileName;
            }
        }

        private string ReadBBSFileData(string BBSName, out string tacticalCallSign)
        {
            string description = "";
            tacticalCallSign = "";

            string filePath = m_OutpostDataDirectory + @"bbs.d\" + BBSName + ".bbs";

            string[] lines = File.ReadAllLines(filePath);
            
            string searchString = "CName=";
            foreach (string line in lines)
            {
                int index = line.IndexOf(searchString);
                if (index != 0)
                    continue;

                tacticalCallSign = line.Substring(searchString.Length, line.Length - searchString.Length - 2); // Skip the two last characters in W2XSC-1
                break;
            }

            searchString = "Desc=";
            foreach (string line in lines)
            {
                int index = line.IndexOf(searchString);
                if (index != 0)
                    continue;

                description = line.Substring(searchString.Length);
                break;
            }
            return description;
        }

        private void WriteBBSDescription(string BBSName)
        {
            string description = textBoxDescription.Text;
            string filePath = m_OutpostDataDirectory + @"bbs.d\" + BBSName + ".bbs";

            string[] lines = File.ReadAllLines(filePath);
            for (int i = 0; i < lines.Length; i++)
            {
                int index = lines[i].IndexOf("Desc=");
                if (index != 0)
                    continue;

                lines[i] = "Desc=" + description;
                break;
            }
            File.WriteAllLines(filePath, lines);
        }

        private void FindLatestRevision()
        {

        }

        private void FindFrequencies()
        {
            string filePath = m_OutpostDataDirectory + @"msg.d\";
            try
            {
                var files = from file in Directory.EnumerateFiles(filePath, "*.oms", SearchOption.AllDirectories)
                            from line in File.ReadLines(file)
                            where line.Contains("Packet Network Frequencies")
                            select new { File = file };

                FileInfo latestFile = null;
                foreach (var f in files)
                {
                    FileInfo fileInfo = new FileInfo(f.File);
                    latestFile = fileInfo;
                }
                // Latest file
                Char[] delimiters = { ' ', ',' };
                int index = 0;
                foreach (string line in File.ReadLines(latestFile.FullName))
                {
                    if (line.Length > 0 && line[0] != '#' && line.Contains("XSC"))
                    {
                        //char[] line2 = new char[line.Length + 1];
                        //// Remove duplicate space and tab characters
                        //for (int i = 0, j = 0; i < line.Length; i++)
                        //{
                        //    if (i < line.Length - 1)
                        //    {
                        //        if ((line[i] == ' ' && line[i + 1] == ' ') || (line[i] == '\t' && line[i + 1] == '\t')
                        //            || (line[i] == ' ' && line[i + 1] == ',') || (line[i] == ',' && line[i + 1] == ' '))
                        //            continue;
                        //        else
                        //        {
                        //            line2[j++] = line[i];
                        //        }
                        //    }
                        //    else
                        //    {
                        //        line2[j++] = line[i];
                        //    }
                        //}
                        //string line3 = new string(line2);
                        //char[] trimChars = { '\0' };
                        //string line4 = line3.TrimEnd(trimChars);
                        //if (line4.Contains("XSC-1"))
                        if (line.Contains("XSC-1"))
                        {
                            //string[] lineElements = line4.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            string[] lineElements = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                            m_Frequencies[index] = string.Format("{0}, {1}", lineElements[2], lineElements[3]);
                            if (index < m_Frequencies.Length - 1)
                                index++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            try
            {
                var files = from file in Directory.EnumerateFiles(filePath, "*.oms", SearchOption.AllDirectories)
                                     from line in File.ReadLines(file)
                                     where line.Contains("Primary Tactical Calls and BBSs")
                            select new { File = file };

                // Find latest file revision
                var lines = from file in files
                            from line in File.ReadLines(file.File)
                            where line.Contains("Last revised")
                            select new {
                                File = file,
                                Line = line
                            };

                Char[] delimiters = { ' ', ',', '\t' };
                DateTime lastRevisionTime = DateTime.MinValue;
                string lastRevisionFile = "";
                foreach (var line in lines)
                {
                    string revision = line.Line;
                    string[] revData = revision.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    // Extract date
                    string[] revDateSplit = revData[3].Split(new char[] {'-'});
                    string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                    int year = Convert.ToInt32(revDateSplit[2]);
                    int month = 1;
                    for (int i = 0; i < months.Length; i++)
                    {
                        if (revDateSplit[1] == months[i])
                        {
                            month = i + 1;
                        }
                    }
                    int day = Convert.ToInt32(revDateSplit[0]);
                    // Extract time
                    string[] timeElements = revData[5].Split(new char[] { ':' });
                    int hour = Convert.ToInt32(timeElements[0]);
                    int min = 0;
                    int sec = 0;
                    if (timeElements.Length < 3)
                        min = Convert.ToInt32(timeElements[1]);
                    if (timeElements.Length > 2)
                        sec = Convert.ToInt32(timeElements[2]);

                    DateTime revDate = new DateTime(year, month, day, hour, min, sec);
                    if (revDate > lastRevisionTime)
                    {
                        lastRevisionTime = revDate;
                        lastRevisionFile = line.File.File;
                    }
                }

                bool W1XSCSecFound = false, W2XSCSecFound = false, W3XSCSecFound = false, W4XSCSecFound = false;
                foreach (string line in File.ReadLines(lastRevisionFile))
                {
                    if (line.Length > 0 && line[0] != '#' && line.Contains("XSC"))
                    {
                        //char[] line2 = new char[line.Length + 1];
                        //for (int i = 0, j = 0; i < line.Length; i++)
                        //{
                        //    // Remove duplicate space and tab characters
                        //    if (i < line.Length - 1)
                        //    {
                        //        if ((line[i] == ' ' && line[i + 1] == ' ') || (line[i] == '\t' && line[i + 1] == '\t'))
                        //            continue;
                        //        else
                        //        {
                        //            line2[j++] = line[i];
                        //        }
                        //    }
                        //    else
                        //    {
                        //        line2[j++] = line[i];
                        //    }
                        //}
                        //string line3 = new string(line2);
                        //char[] trimChars = { '\0' };
                        //string line4 = line3.TrimEnd(trimChars);
                        string line4 = line.TrimEnd(new char[] {'\0'});
                        string[] lineElements = line4.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                        if (lineElements[lineElements.Length - 2].Contains("W1XSC"))
                        {
                            m_Secondaries[0] = lineElements[lineElements.Length - 1];
                            W1XSCSecFound = true;
                        }
                        else if (line4.Contains("W2XSC"))
                        {
                            m_Secondaries[1] = lineElements[lineElements.Length - 1];
                            W2XSCSecFound = true;
                        }
                        else if (line4.Contains("W3XSC"))
                        {
                            m_Secondaries[2] = lineElements[lineElements.Length - 1];
                            W3XSCSecFound = true;
                        }
                        else if (line4.Contains("W4XSC"))
                        {
                            m_Secondaries[3] = lineElements[lineElements.Length - 1];
                            W4XSCSecFound = true;
                        }
                        else if (line4.Contains("W5XSC"))
                        {
                            MessageBox.Show("New secondary tactical callsign, {0}. This application needs to be updated", "W5XSC");
                        }
                        else if (line4.Contains("W6XSC"))
                        {
                            MessageBox.Show("New secondary tactical callsign, {0}. This application needs to be updated", "W6XSC");
                        }
                        if (W1XSCSecFound && W2XSCSecFound && W3XSCSecFound && W4XSCSecFound)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
