using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml;

namespace DependencyUpdater
{
    internal class VersionRetriever
    {
        private static void UpdateXmlFile(string filename)
        {
            Regex packageReference = new Regex("PackageReference Include=\".+\" Version=\"[0-9]+.[0-9]+.[0-9]+-");

            Regex updatedVersionToReplace = new Regex("Version=\"[0-9]+.[0-9]+.[0-9]+-");
            var xmlNodesToReplace = new Dictionary<string, string>();

            using (StreamReader reader = new StreamReader(filename))
            {
                string node;
                while ((node = reader.ReadLine()) != null)
                {
                    // Try to match each line against the Regex.
                    Match match = packageReference.Match(node);
                    if (match.Success)
                    {
                        var packageFromFile = GetPackageNameFromXmlNode(node);
                        var versionFromFile = GetVersionNameFromXmlNode(node, updatedVersionToReplace);

                        var versionFromFeed = GetVersionFromFeed(packageFromFile);

                        // Update the version in file only if version from feed is higher
                        if (versionFromFeed[0] >= versionFromFile[0])
                        {
                            var rightVersion = $@"Version=""{versionFromFeed[0].ToString() + "." + versionFromFeed[1].ToString() + "." + versionFromFeed[2].ToString()}-";
                            var updatedXmlNode = updatedVersionToReplace.Replace(node, rightVersion);
                            if (!xmlNodesToReplace.ContainsKey(node))
                            {
                                xmlNodesToReplace.Add(node, updatedXmlNode);
                            }
                        }

                        if (versionFromFeed[0] == versionFromFile[0] &&
                            versionFromFeed[1] > versionFromFile[1])
                        {
                            var rightVersion = $@"Version=""{versionFromFeed[0].ToString() + "." + versionFromFeed[1].ToString() + "." + versionFromFeed[2].ToString()}-";
                            var updatedXmlNode = updatedVersionToReplace.Replace(node, rightVersion);
                            if (!xmlNodesToReplace.ContainsKey(node))
                            {
                                xmlNodesToReplace.Add(node, updatedXmlNode);
                            }
                        }

                        if (versionFromFeed[0] == versionFromFile[0] &&
                            versionFromFeed[1] == versionFromFile[1] &&
                            versionFromFeed[2] > versionFromFile[2])
                        {
                            var rightVersion = $@"Version=""{versionFromFeed[0].ToString() + "." + versionFromFeed[1].ToString() + "." + versionFromFeed[2].ToString()}-";
                            var updatedXmlNode = updatedVersionToReplace.Replace(node, rightVersion);
                            if (!xmlNodesToReplace.ContainsKey(node))
                            {
                                xmlNodesToReplace.Add(node, updatedXmlNode);
                            }
                        }
                    }
                }
            }

            foreach (var xmlNodeToReplace in xmlNodesToReplace)
            {
                UpdatePackageVersion(filename, xmlNodeToReplace);
            }
        }

        private static void UpdatePackageVersion(string filename, KeyValuePair<string, string> updatedXmlNode)
        {
            string fileData = File.ReadAllText(filename);
            fileData = fileData.Replace(updatedXmlNode.Key, updatedXmlNode.Value);
            File.WriteAllText(filename, fileData);
        }

        public static List<int> GetVersionNameFromXmlNode(string line, Regex regex)
        {
            Regex version = new Regex("[0-9]+");
            var versionCombiner = new List<int>();

            var versionWithDash = regex.Match(line).ToString();
            MatchCollection versionPieces = version.Matches(versionWithDash);

            foreach (Match versionPiece in versionPieces)
            {
                versionCombiner.Add(int.Parse(versionPiece.Value));
            }

            return versionCombiner;
        }

        static void WalkDirectoryTree(DirectoryInfo root)
        {
            FileInfo[] files = null;
            DirectoryInfo[] subDirs = null;

            try
            {
                files = root.GetFiles("*.csproj");
            }
            catch (UnauthorizedAccessException e)
            {
                //log.Add(e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }

            if (files.Any())
            {
                foreach (var fileInfo in files)
                {
                    UpdateXmlFile(fileInfo.FullName);
                    Console.WriteLine(fileInfo.FullName);
                }
            }

            subDirs = root.GetDirectories();

            foreach (var directoryInfo in subDirs)
            {
                WalkDirectoryTree(directoryInfo);
            }
        }

        static List<int> GetVersionFromFeed(string package)
        {
            var apiUrl = $"http://nuget1dk1/nuget/9.3.0_master/FindPackagesById()?id='{package}'";
            var responseStream = GetFeedResponseStream(apiUrl);

            var document = new XmlDocument();

            Regex packageName = new Regex(package);
            Regex versionRetriever = new Regex("Version='.*-");
            Regex version = new Regex("[0-9]+");

            var nodes = new List<string>();
            var packageVersionFilter = new Dictionary<int, List<int>>();
            var versionCombiner = new List<int>();

            var majorVersion = 0;
            var minorVersion = 0;
            var patchVersion = 0;

            if (responseStream != null)
            {
                document.Load(responseStream);

                // Get all xml nodes with package name and all existing versions
                if (document.DocumentElement != null)
                {
                    var documentElementChildNodes = document.DocumentElement.ChildNodes;

                    foreach (XmlNode xmlNode in documentElementChildNodes)
                    {
                        if (xmlNode.Name == "entry")
                        {
                            foreach (XmlNode node in xmlNode.ChildNodes)
                            {
                                if (node.Name == "id")
                                {
                                    nodes.Add(node.InnerText);
                                }
                            }
                        }
                    }
                }

                // put all existing package versions in dictionary
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (packageName.IsMatch(nodes[i]))
                    {
                        var versionWithString = versionRetriever.Match(nodes[i]).ToString();
                        MatchCollection wholeVersionAsString = version.Matches(versionWithString);

                        foreach (Match number in wholeVersionAsString)
                        {
                            if (!packageVersionFilter.ContainsKey(i))
                            {
                                packageVersionFilter.Add(i, new List<int>()
                            {
                                int.Parse(number.Value)
                            });
                                continue;
                            }
                            packageVersionFilter[i].Add(int.Parse(number.Value));
                        }
                    }
                }

                // Filter found versions and find the newest
                foreach (var pair in packageVersionFilter)
                {
                    if (pair.Value[0] > majorVersion)
                    {
                        majorVersion = pair.Value[0];
                    }
                    else if (pair.Value[0] == majorVersion && pair.Value[1] > minorVersion)
                    {
                        minorVersion = pair.Value[1];
                    }
                    else if (pair.Value[0] == majorVersion && pair.Value[1] == minorVersion & pair.Value[2] > patchVersion)
                    {
                        patchVersion = pair.Value[2];
                    }
                }

                // Adding to List<int> to have the ability to compare with existing in a file
                versionCombiner.Add(majorVersion);
                versionCombiner.Add(minorVersion);
                versionCombiner.Add(patchVersion);
            }

            return versionCombiner;
        }

        private static Stream GetFeedResponseStream(string apiUrl)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);

            request.Method = "GET";

            // Use Windows credentials to log in to nuget feed
            request.UseDefaultCredentials = true;

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            return response.GetResponseStream();
        }

        public static string GetPackageNameFromXmlNode(string line)
        {
            Regex packageNameSubstring = new Regex("\\w+.+\"");

            // Remove opening tag with package reference words
            var nodeWithoutOpeningTag = line.Substring(31);

            // Remove close tag
            var nodeWithoutCloseTag = nodeWithoutOpeningTag.Remove(nodeWithoutOpeningTag.Length - 4);

            // Matched package name with one closing quote
            var packageNameWithQuote = packageNameSubstring.Match(nodeWithoutCloseTag).ToString();

            return packageNameWithQuote.Remove(packageNameWithQuote.Length - 11);
        }
    }
}