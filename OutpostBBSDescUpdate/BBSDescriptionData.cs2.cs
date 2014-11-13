using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

//namespace OutpostBBSDescUpdate
//{
    public partial class BBSDescriptionData
    {
        public void WriteBBSDescriptionDataToFile(string filePath, out string sError)
        {
            sError = "";
            
            using (FileStream myFileStream = new FileStream(filePath, FileMode.Create))
            {
                try
                {
                    XmlSerializer mySerializer = new XmlSerializer(typeof(BBSDescriptionData));
                    mySerializer.Serialize(myFileStream, this);
                }
                catch (Exception e)
                {
                    sError = e.ToString();
                }
            }
        }

        public void ReadBBSDescriptionDataFromFile(string filePath, out string sError)
        {
            sError = "";

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                using (FileStream myFileStream = new FileStream(filePath, FileMode.Open))
                {
                    try
                    {
                        XmlSerializer mySerializer = new XmlSerializer(typeof(BBSDescriptionData));
                        BBSDescriptionData bbsDescriptionData = (BBSDescriptionData)mySerializer.Deserialize(myFileStream);
                        
                        this.FrequenciesRevisionTime = bbsDescriptionData.FrequenciesRevisionTime;
                        this.PrimaryBBSsRevisionTime = bbsDescriptionData.PrimaryBBSsRevisionTime;
                        this.TacticalCallSign = bbsDescriptionData.TacticalCallSign;
                        for (int i = 0; i < bbsDescriptionData.TacticalCallSign.Length; i++)
                        {
                            BBSDescriptionDataTacticalCallSign tacticalCallSign = new BBSDescriptionDataTacticalCallSign();
                            tacticalCallSign.frequencies = bbsDescriptionData.TacticalCallSign[i].frequencies;
                            tacticalCallSign.OriginalDescription = new BBSDescriptionDataTacticalCallSignOriginalDescription();
                            tacticalCallSign.OriginalDescription.description = bbsDescriptionData.TacticalCallSign[i].OriginalDescription.description;
                            tacticalCallSign.primary = bbsDescriptionData.TacticalCallSign[i].primary;
                            tacticalCallSign.secondary = bbsDescriptionData.tacticalCallSignField[i].secondary;
                            tacticalCallSign.NewDescription = new BBSDescriptionDataTacticalCallSignNewDescription();
                            tacticalCallSign.NewDescription.description = bbsDescriptionData.TacticalCallSign[i].NewDescription.description;
                            this.TacticalCallSign[i] = tacticalCallSign;
                        }
                    }
                    catch (Exception e)
                    {
                        sError = e.ToString();
                    }
                }
            }
            else
            {
                sError = "File does not exist, or it is empty.";
            }
        }

        public static void ReadBBSDescriptionDataFromFile(string filePath, ref BBSDescriptionData bbsDescriptionData, out string sError)
        {
            sError = "";

            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists && fileInfo.Length > 0)
            {
                using (FileStream myFileStream = new FileStream(filePath, FileMode.Open))
                {
                    try
                    {
                        XmlSerializer mySerializer = new XmlSerializer(typeof(BBSDescriptionData));
                        bbsDescriptionData = (BBSDescriptionData)mySerializer.Deserialize(myFileStream);
                    }
                    catch (Exception e)
                    {
                        sError = e.ToString();
                    }
                }
            }
            else
            {
                sError = "File does not exist, or the length is zero.";
            }
        }

    }
//}
