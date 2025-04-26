using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Configuration;
using IniParser.Model;

namespace ArchiveCacheManager
{
    public class GameIndex
    {
        private readonly string SelectedFile = "SelectedFile";

        private static IniData mGameIndex = null;

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
                var parser = new IniDataParser();
                mGameIndex = new IniData();

                try
                {
                    var reader = new StreamReader(gameIndexPath);
                    mGameIndex = parser.Parse(reader);
                }
                catch (Exception e)
                {
                    Logger.Log(string.Format("Error parsing game index file from {0}. Deleting invalid file.", gameIndexPath));
                    Logger.Log(e.ToString(), Logger.LogLevel.Exception);
                    File.Delete(gameIndexPath);
                    mGameIndex = null;
                }
            }
        }

        public static void Save()
        {
            string gameIndexPath = PathUtils.GetPluginGameIndexPath();

            if (mGameIndex != null)
            {
                var formatter = new IniDataFormatter();
                var configuration = new IniFormattingConfiguration();

                try
                {
                    using var writer = new StreamWriter(gameIndexPath);
                    writer.Write(formatter.Format(mGameIndex, configuration));
                    writer.Flush();
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

            if (mGameIndex != null && mGameIndex.Sections.Contains(gameId))
            {
                selectedFile = mGameIndex[gameId][nameof(SelectedFile)] ?? string.Empty;
            }

            return selectedFile;
        }

        public static void SetSelectedFile(string gameId, string selectedFile)
        {
            if (mGameIndex == null)
            {
                mGameIndex = new IniData();
                mGameIndex.CreateSectionsIfTheyDontExist = true;
            }

            mGameIndex[gameId][nameof(SelectedFile)] = selectedFile;

            Save();
        }
    }
}
