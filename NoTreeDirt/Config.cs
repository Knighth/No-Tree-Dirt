using System;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

namespace NoTreeDirt
{
    public class Configuration
    {
        public bool DebugLogging = false;   
        public byte DebugLoggingLevel = 0;  //detail: 1 basically very similar to just on+0 ; 2 = Very detailed; 3+ extreme only meant for me during dev...if that. 
        public bool UpdateTreeAssets = true;
        public bool UpdateResetTrees = true;
        public bool ResetExistingProps = false; //has no effect atm
        public bool UpdateExistingProps = false; //has no effect atm.
        public bool UseCustomLogFile = false;
        public string CustomLogFilePath = "NoTreeDirt_Log.txt";

        public Configuration() { }


        public static void Serialize(string filename, Configuration config)
        {
            var serializer = new XmlSerializer(typeof(Configuration));
            try
            {
                using (var writer = new StreamWriter(filename))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch (System.IO.IOException ex1)
            {
                Helper.dbgLog("Filesystem or IO Error: \r\n", ex1, true);
            }
            catch (Exception ex1)
            {
                Helper.dbgLog(ex1.Message.ToString() + "\r\n", ex1, true);
            }
        }

        public static Configuration Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(Configuration));

            try
            {
                using (var reader = new StreamReader(filename))
                {
                    var config = (Configuration)serializer.Deserialize(reader);
                    ValidateConfig(ref config);
                    return config;
                }
            }
            
            catch(System.IO.FileNotFoundException ex4)
            {
                Helper.dbgLog("File not found. This is expected if no config file. \r\n",ex4,false);
            }

            catch (System.IO.IOException ex1)
            {
                Helper.dbgLog("Filesystem or IO Error: \r\n",ex1,true);
            }
            catch (Exception ex1)
            {
                Helper.dbgLog(ex1.Message.ToString() + "\r\n",ex1,true);
            }

            return null;
        }

        /// <summary>
        /// Constrain certain values read in from the config file that will either cause issue or just make no sense. 
        /// </summary>
        /// <param name="tmpConfig"> An instance of an initialized Configuration object (byref)</param>

        public static void ValidateConfig(ref Configuration tmpConfig)
        {
        }
    }
}
