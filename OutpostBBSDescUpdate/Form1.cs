using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        string[] m_BBSNames;
        const string c_MessageBoxCaption = "BBS Description Update";

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
                MessageBox.Show(string.Format("Outpost Data Directory can not be found. \n{0}\n Outpost may not be installed.", m_OutpostDataDirectory), c_MessageBoxCaption, MessageBoxButtons.OK);
            }

            index = m_sUserDataPath.IndexOf("OutpostBBSDescUpdate");
            index = m_sUserDataPath.IndexOf('\\', index);
            m_UserDataDirectory = m_sUserDataPath.Substring(0, index + 1);
            m_sUserDataPath = m_UserDataDirectory + @"BBSData.xml";

            
            string BBSDirectory = m_OutpostDataDirectory + @"bbs.d";
            var bbsFiles = Directory.EnumerateFiles(BBSDirectory, "*.bbs", SearchOption.AllDirectories);

            // Initialize some datastructures
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
            m_BBSNames = new string[bbsFiles.Count()];

            string outpostVersion = CheckOutpostVersion();

            // Initialize BBS Name ComboBox with names
            int BBSNameIndex = 0;
            foreach (var file in bbsFiles)
            {
                FileInfo fileInfo = new FileInfo(file);
                string fileName = fileInfo.Name;
                index = fileName.LastIndexOf('.');
                m_BBSNames[BBSNameIndex] = fileName.Substring(0, index);
                comboBoxBBSName.Items.Add(new ComboBoxBBSNameItem(m_BBSNames[BBSNameIndex], BBSNameIndex));
                BBSNameIndex++;
            }

            DateTime frequenciesRevisionTime;
            DateTime bbsRevisionTime;
            FindFrequencies(out frequenciesRevisionTime, out bbsRevisionTime);

            string sError = "";
            string newFrequency = "";
            string newSecondary = "";
            BBSNameIndex = 0;
            m_BBSDescriptionData = new BBSDescriptionData();
            if (File.Exists(m_sUserDataPath))
            {
                // Work on existing data
                BBSDescriptionData.ReadBBSDescriptionDataFromFile(m_sUserDataPath, ref m_BBSDescriptionData, out sError);
                //m_BBSDescriptionData.ReadBBSDescriptionDataFromFile(m_sUserDataPath, out sError);
                if (sError.Length > 0)
                {
                    MessageBox.Show(sError + "\nExiting.", c_MessageBoxCaption, MessageBoxButtons.OK);
                    System.Environment.Exit(1);
                }
                // Get current description in Outpost
                BBSNameIndex = 0;
                foreach (string file in bbsFiles)
                {
                    string callSign;        // Not used here
                    string BBSDescription = ReadBBSFileData(m_BBSNames[BBSNameIndex], out callSign);

                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].CurrentDescription.description = BBSDescription;

                    BBSNameIndex++;
                }
                // Check if new files are available.
                if (frequenciesRevisionTime > m_BBSDescriptionData.FrequenciesRevisionTime || bbsRevisionTime > m_BBSDescriptionData.PrimaryBBSsRevisionTime)
                {
                    // Update revision times and new description
                    m_BBSDescriptionData.FrequenciesRevisionTime = frequenciesRevisionTime;
                    m_BBSDescriptionData.PrimaryBBSsRevisionTime = bbsRevisionTime;
                    newFrequency = "";
                    if (m_Frequencies[BBSNameIndex].Length > 0)
                        newFrequency = "\r\nFrequencies: " + m_Frequencies[BBSNameIndex] + ".";
                    newSecondary = "";
                    if (m_Secondaries[BBSNameIndex].Length > 0)
                        newSecondary = "\r\nSecondary Call Sign: " + m_Secondaries[BBSNameIndex] + ".";
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].NewDescription.description =
                        m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].OriginalDescription.description + newFrequency + newSecondary;
                    buttonSave.Enabled = true;
                }
            }
            else
            {
                // Create first time data structure
                m_BBSDescriptionData.OutpostVersion = outpostVersion;
                m_BBSDescriptionData.FrequenciesRevisionTime = frequenciesRevisionTime;
                m_BBSDescriptionData.PrimaryBBSsRevisionTime = bbsRevisionTime;
                BBSDescriptionDataTacticalCallSign[] tacticalCallSigns = new BBSDescriptionDataTacticalCallSign[bbsFiles.Count()];
                m_BBSDescriptionData.TacticalCallSigns = tacticalCallSigns;
                foreach (string file in bbsFiles)
                {
                    string callSign;
                    string BBSDescription = ReadBBSFileData(m_BBSNames[BBSNameIndex], out callSign);

                    BBSDescriptionDataTacticalCallSign tacticalCallSign = new BBSDescriptionDataTacticalCallSign();
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex] = tacticalCallSign;
                    BBSOriginalDescription originalDescription = new BBSOriginalDescription();
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].OriginalDescription = originalDescription;
                    BBSCurrentDescription currentDescription = new BBSCurrentDescription();
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].CurrentDescription = currentDescription;
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].OriginalDescription.description = BBSDescription;
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].CurrentDescription.description = BBSDescription;
                    BBSNewDescription newDescription = new BBSNewDescription();
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].NewDescription = newDescription;
                    newFrequency = "";
                    if (m_Frequencies[BBSNameIndex].Length > 0)
                        newFrequency = "\r\nFrequencies: " + m_Frequencies[BBSNameIndex] + ".";
                    newSecondary = "";
                    if (m_Secondaries[BBSNameIndex].Length > 0)
                        newSecondary = "\r\nSecondary Call Sign: " + m_Secondaries[BBSNameIndex] + ".";
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].NewDescription.description = BBSDescription + newFrequency + newSecondary;
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].primary = callSign;
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].secondary = m_Secondaries[BBSNameIndex];
                    m_BBSDescriptionData.TacticalCallSigns[BBSNameIndex].frequencies = m_Frequencies[BBSNameIndex];

                    BBSNameIndex++;
                }
                //m_BBSDescriptionData.WriteBBSDescriptionDataToFile(m_sUserDataPath, out sError);
                //if (sError.Length > 0)
                //{
                //    MessageBox.Show(sError + "\nExiting.", c_MessageBoxCaption, MessageBoxButtons.OK);
                //    System.Environment.Exit(1);
                //}
                buttonSave.Enabled = true;
            }
            if (m_BBSDescriptionData.TacticalCallSigns.Length != comboBoxBBSName.Items.Count)
            {
                MessageBox.Show("New tactical callsign(s) added.", c_MessageBoxCaption, MessageBoxButtons.OK);
            }
            comboBoxBBSName.SelectedIndex = 0;
        }

        private string CheckOutpostVersion()
        {
            string version = "";


            // Check if a new version of Outpost is installed
            string filePath = m_OutpostDataDirectory + "Outpost.glo";

            foreach (string line in File.ReadLines(filePath))
            {
                //Version=3.0.0 c144
                if (line.IndexOf("Version=") == 0)
                {
                    string[] sections = line.Split(new char[] { '=', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    version = sections.First(section => Char.IsNumber(section, 0));

                    if (version == "3.0.0")
                        break;
                    else
                    {
                        MessageBox.Show(string.Format("This version ({0}) of Outpost is not supported.", version), c_MessageBoxCaption, MessageBoxButtons.OK);
                        System.Environment.Exit(1);
                    }
                }
            }
            return version;
        }

        private void comboBoxBBSName_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = comboBoxBBSName.SelectedItem as string;
            // read from temporary data
            var item = comboBoxBBSName.SelectedItem as ComboBoxBBSNameItem;
            textBoxDescription.Text = m_BBSDescriptionData.TacticalCallSigns[item.BBSNameIndex].NewDescription.description;
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            string sError = "";
            m_BBSDescriptionData.WriteBBSDescriptionDataToFile(m_sUserDataPath, out sError);
            if (sError.Length > 0)
            {
                MessageBox.Show(sError + "\nExiting.", c_MessageBoxCaption, MessageBoxButtons.OK);
                System.Environment.Exit(1);
            }

            for (int i = 0; i < m_BBSNames.Length; i++)
            {
                string description = m_BBSDescriptionData.TacticalCallSigns[i].NewDescription.description;
                // Remove all "\r" before writing to Outpost file
                string descFilt = Regex.Replace(description, @"\r\n", @"\n");
                WriteBBSDescription(m_BBSNames[i], descFilt);
            }
            buttonSave.Enabled = false;
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        // Parse a bbs file for description and primary BBS call sign.
        // Returns description
        private string ReadBBSFileData(string BBSName, out string tacticalCallSign)
        {
            string description = "";
            tacticalCallSign = "";

            string filePath = m_OutpostDataDirectory + @"bbs.d\" + BBSName + ".bbs";

            string[] lines = File.ReadAllLines(filePath);
            
            string searchString = "CName=";
            string callSignLine = lines.First(line => line.IndexOf(searchString) == 0);
            tacticalCallSign = callSignLine.Substring(searchString.Length, callSignLine.Length - searchString.Length - 2);

            searchString = "Desc=";
            string descLine = lines.First(line => line.IndexOf(searchString) == 0);
            description = descLine.Substring(searchString.Length);
            return description;
        }

        private void WriteBBSDescription(string BBSName, string description)
        {
            string filePath = m_OutpostDataDirectory + @"bbs.d\" + BBSName + ".bbs";

            string searchString = "Desc=";
            string[] lines = File.ReadAllLines(filePath);
            int i = 0;
            lines.First(s => lines[i++].IndexOf(searchString) == 0);
            lines[i - 1] = searchString + description;

            File.WriteAllLines(filePath, lines);
        }

        // Returns the path to the latest file and the revision time
        private string FindLatestRevisionFile(string filePath, string fileDescription, out DateTime revisionTime)
        {
            DateTime latestRevTime = DateTime.MinValue;
            revisionTime = latestRevTime;
            try
            {
                // Find all files of type "fileDescription"
                var files = from file in Directory.EnumerateFiles(filePath, "*.oms", SearchOption.AllDirectories)
                            from line in File.ReadLines(file)
                            where line.Contains(fileDescription)
                            select new { File = file };

                // Find latest file revision
                var lines = from file in files
                            from line in File.ReadLines(file.File)
                            where line.Contains("Last revised")
                            select new
                            {
                                File = file,
                                Line = line
                            };

                Char[] delimiters = { ' ', ',', '\t' };
                string lastRevisionFile = "";
                foreach (var line in lines)
                {
                    string revision = line.Line;
                    string[] revData = revision.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                    try
                    { 
                        // Extract date
                        int dateIndex = 0;
                        revData.First(rev => Char.IsNumber(revData[dateIndex++], 0) == true);
                        dateIndex--;
                        string[] revDateSplit = revData[dateIndex].Split(new char[] { '-' });
                        string[] months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
                        int year = Convert.ToInt32(revDateSplit[2]);
                        int index = 0;
                        months.First(month => months[index++] == revDateSplit[1]);
                        int monthIndex = index;
                        int day = Convert.ToInt32(revDateSplit[0]);
                        // Extract time
                        string[] timeElements = revData[dateIndex + 2].Split(new char[] { ':' });
                        int hour = Convert.ToInt32(timeElements[0]);
                        int min = 0;
                        int sec = 0;
                        if (timeElements.Length < 3)
                            min = Convert.ToInt32(timeElements[1]);
                        if (timeElements.Length > 2)
                            sec = Convert.ToInt32(timeElements[2]);

                        DateTime revDate = new DateTime(year, monthIndex, day, hour, min, sec);
                        if (revDate > latestRevTime)
                        {
                            revisionTime = revDate;
                            lastRevisionFile = line.File.File;
                        }
                    }
                    catch
                    {
                        MessageBox.Show("Can not determine revision date of bulletin. \nExiting.", c_MessageBoxCaption, MessageBoxButtons.OK);
                    }
                }
                return lastRevisionFile;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + "\nExiting.", c_MessageBoxCaption, MessageBoxButtons.OK);
                System.Environment.Exit(1);
            }
            return  "";
        }

        private void FindFrequencies(out DateTime frequenciesRevisionTime, out DateTime bbsRevisionTime)
        {
            frequenciesRevisionTime = DateTime.MinValue;
            bbsRevisionTime = DateTime.MinValue;

            string filePath = m_OutpostDataDirectory + @"msg.d\";

            string latestRevFile = FindLatestRevisionFile(filePath, "Packet Network Frequencies", out frequenciesRevisionTime);

            try
            {
                Char[] delimiters = { ' ', ',' };
                int index = 0;
                foreach (string line in File.ReadLines(latestRevFile))
                {
                    if (line.Length > 0 && line[0] != '#' && line.Contains("XSC-1"))
                    {
                        string[] lineElements = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                        // Make sure we have frequency as a number
                        if (Char.IsNumber(lineElements[2], 0) && Char.IsNumber(lineElements[3], 0))
                        {
                            m_Frequencies[index] = string.Format("{0}, {1}", lineElements[2], lineElements[3]);
                        }
                        if (index < m_Frequencies.Length - 1)
                            index++;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, c_MessageBoxCaption, MessageBoxButtons.OK);
            }

            latestRevFile = FindLatestRevisionFile(filePath, "Primary Tactical Calls and BBSs", out bbsRevisionTime);

            try
            {

                bool W1XSCSecFound = false, W2XSCSecFound = false, W3XSCSecFound = false, W4XSCSecFound = false;
                foreach (string line in File.ReadLines(latestRevFile))
                {
                    if (line.Length > 0 && line[0] != '#' && line.Contains("XSC"))
                    {
                        Char[] delimiters = { ' ', ',', '\t' };
                        string[] lineElements = line.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);

                        if (lineElements[lineElements.Length - 2].Contains("W1XSC"))
                        {
                            m_Secondaries[0] = lineElements[lineElements.Length - 1];
                            W1XSCSecFound = true;
                        }
                        else if (line.Contains("W2XSC"))
                        {
                            m_Secondaries[1] = lineElements[lineElements.Length - 1];
                            W2XSCSecFound = true;
                        }
                        else if (line.Contains("W3XSC"))
                        {
                            m_Secondaries[2] = lineElements[lineElements.Length - 1];
                            W3XSCSecFound = true;
                        }
                        else if (line.Contains("W4XSC"))
                        {
                            m_Secondaries[3] = lineElements[lineElements.Length - 1];
                            W4XSCSecFound = true;
                        }
                        else if (line.Contains("W5XSC"))
                        {
                            MessageBox.Show("New secondary tactical callsign, {0}. This application needs to be updated", "W5XSC");
                        }
                        else if (line.Contains("W6XSC"))
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
                MessageBox.Show(e.Message, c_MessageBoxCaption, MessageBoxButtons.OK);
            }
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < m_BBSDescriptionData.TacticalCallSigns.Length; i++)
            {
                m_BBSDescriptionData.TacticalCallSigns[i].NewDescription.description =
                    m_BBSDescriptionData.TacticalCallSigns[i].OriginalDescription.description;
            }
            // Update current description
            textBoxDescription.Text = m_BBSDescriptionData.TacticalCallSigns[comboBoxBBSName.SelectedIndex].NewDescription.description;
            buttonSave.Enabled = true;
        }

        private void textBoxDescription_TextChanged(object sender, EventArgs e)
        {
            if (m_BBSDescriptionData.TacticalCallSigns[comboBoxBBSName.SelectedIndex].CurrentDescription.description != textBoxDescription.Text)
            {
                buttonSave.Enabled = true;
            }
            else
            {
                buttonSave.Enabled = false;
            }
        }

    }
}
