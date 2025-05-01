using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Salaros.Configuration;

namespace ArchiveCacheManager
{
    public class GameIndex
    {
        private static ConfigParser mGameIndex = null;

        static GameIndex()
        {
            Load();
        }

        public static void Load()
        {
            string gameIndexPath = PathUtils.GetPluginGameIndexPath();
            mGameIndex = null;

            if (File.Exists(gameIndexPath))
            {
                try
                {
                    mGameIndex = new ConfigParser(gameIndexPath);
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Error parsing game index file from {0}.", gameIndexPath));
                    Logger.Log(e.ToString(), Logger.LogLevel.Exception);
                    //                    File.Delete(gameIndexPath);
                    //                    mGameIndex = null;
                }
            }
        }

        public static void Save()
        {
            string gameIndexPath = PathUtils.GetPluginGameIndexPath();

            if (mGameIndex != null)
            {
                try
                {
                    mGameIndex.Save(gameIndexPath);
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Error saving game index file to {0}.", gameIndexPath));
                    Logger.Log(e.ToString(), Logger.LogLevel.Exception);
                }
            }
        }

        public static string GetSelectedFile(string gameId)
        {
            string selectedFile = string.Empty;

            if (mGameIndex != null)
            {
                selectedFile = mGameIndex[gameId]["SelectedFile"];
                Logger.Log(string.Format("selected {1} for Gameid {0} in index.", gameId, selectedFile));
            }

            return selectedFile;
        }

        public static void SetSelectedFile(string gameId, string selectedFile)
        {
            if (mGameIndex == null)
            {
                mGameIndex = new ConfigParser();
            }
            Logger.Log(string.Format("writing selected {1} for {0} in index.", gameId, selectedFile));
            mGameIndex.SetValue(gameId, "SelectedFile", string.Format("\"{0}\"", selectedFile));

            Save();
        }
    }
}
