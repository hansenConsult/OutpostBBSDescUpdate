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
