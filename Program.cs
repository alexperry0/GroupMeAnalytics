using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GroupMeAnalytics {
    class Program {
        static void Main(string[] args) {

            var client = new RestClient("https://api.groupme.com/v3");
            var accessToken = ConfigurationManager.AppSettings["accessToken"];
            var cacheFilePathBaseDir = ConfigurationManager.AppSettings["cacheFileLocation"];

            var groups = getGroups(client, accessToken).response.ToList();

            foreach (var group in groups) {
                Console.WriteLine($"Members of \"{group.name}\"");
                foreach (var member in group.members) {
                    Console.WriteLine($"{member.name} {member.user_id}");
                }

                var cacheFileForSpecificGroup = cacheFilePathBaseDir + (group.name);
                var messages = getAllMessages(client, accessToken, cacheFileForSpecificGroup, group);
                (var likers, var likees, var totalMessagesPerUser, var likerToTotalMessages, var likeeToTotalMessages) = analyzeLikes(group.members, messages);

                Console.WriteLine("\nLikes Given");
                foreach (var liker in likers.OrderByDescending(key => key.Value)) {
                    Console.WriteLine($"{liker.Key} - {liker.Value}");
                }

                Console.WriteLine("\nLikes Received");
                foreach (var likee in likees.OrderByDescending(key => key.Value)) {
                    Console.WriteLine($"{likee.Key} - {likee.Value}");
                }

                Console.WriteLine("\nMessages Sent");
                foreach (var message in totalMessagesPerUser.OrderByDescending(key => key.Value)) {
                    Console.WriteLine($"{message.Key} - {message.Value}");
                }

                Console.WriteLine("\nLikes Given / Total Messages Sent by Member");
                foreach (var ratio in likerToTotalMessages.OrderByDescending(key => key.Value)) {
                    Console.WriteLine($"{ratio.Key} - {ratio.Value.ToString("0.#####")}:1");
                }

                Console.WriteLine("\nLikes Received / Total Messages Sent by Member");
                foreach (var ratio in likeeToTotalMessages.OrderByDescending(key => key.Value)) {
                    Console.WriteLine($"{ratio.Key} - {ratio.Value.ToString("0.#####")}:1");
                }

                Console.WriteLine("\n\n\n");
            }
            
            

            Console.ReadLine();
        }

        private static Groups getGroups(RestClient client, string accessToken) {
            var requester = new RestRequest("/groups");
            requester.AddParameter("token", accessToken);
            var response = client.Get(requester);

            var groups = JsonConvert.DeserializeObject<Groups>(response.Content);
            return groups;

        }

        private static List<Message> getMessages(RestClient client, string accessToken, string groupId, string after_id) {
            var requester = new RestRequest("/groups/{group_id}/messages");
            requester.AddUrlSegment("group_id", groupId.ToString());
            requester.AddParameter("limit", 100);
            requester.AddParameter("after_id", after_id);
            requester.AddParameter("token", accessToken);
            var response = client.Get(requester);

            var messages = JsonConvert.DeserializeObject<Messages>(response.Content);
            return messages.response.messages;
        }

        public static void CacheMessageData<T>(string filePath, T objectToWrite, bool append = false) where T : new() {
            TextWriter writer = null;
            try {
                var contentsToWriteToFile = JsonConvert.SerializeObject(objectToWrite);
                writer = new StreamWriter(filePath, append);
                writer.Write(contentsToWriteToFile);
            } finally {
                if (writer != null)
                    writer.Close();
            }
        }

        public static T ReadCachedMessageData<T>(string filePath) where T : new() {
            TextReader reader = null;
            try {
                reader = new StreamReader(filePath);
                var fileContents = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(fileContents);
            } finally {
                if (reader != null)
                    reader.Close();
            }
        }

        private static List<Message> getAllMessages(RestClient client, string accessToken, string cacheFilePath, GroupResponse groupMetaData) {
            List<Message> messages = new List<Message>();
            if (File.Exists(cacheFilePath)) 
                messages = ReadCachedMessageData<List<Message>>(cacheFilePath);
            else
                Console.WriteLine("No cache file found. Downloading message data. This may take a while.");

            var idOfLastReadMessage = messages.Count > 0 ? messages.Last().id : "0";
            
            var moreMessagesToGet = true;
            var messagesDownloaded = 0;
            while (moreMessagesToGet) {
                var response = getMessages(client, accessToken, groupMetaData.group_id, idOfLastReadMessage.ToString());
                idOfLastReadMessage = response.Count > 0 ? response.Last().id : "0";
                messages.AddRange(response);
                if (response.Count < 100) {
                    moreMessagesToGet = false;
                }

                /////////////// For debugging
                //if (messages.Count >= 500) {
                //    moreMessagesToGet = false;
                //}
                messagesDownloaded += response.Count;
                ///////////////
            }

            if (messagesDownloaded > 0)
                CacheMessageData(cacheFilePath, messages);
            
            return messages;
        }

        private static (Dictionary<string, int> liker, Dictionary<string, int> likee, Dictionary<string, int> totalMessagesPerUser, Dictionary<string, double> r1, Dictionary<string, double> r2) analyzeLikes(List<GroupMember> members, List<Message> messages) {
            var liker = new Dictionary<string, int>();
            var likee = new Dictionary<string, int>();
            var totalMessagesPerUser = new Dictionary<string, int>();
            var dict = new Dictionary<string, string>();

            foreach (var message in messages) {
                RegisterMemberNameAndId(message.user_id, message.name, ref dict);
            }

            var memberIdtoNameMapping = dict;

            foreach (var message in messages) {

                // Get whose like the most
                if (likee.ContainsKey(memberIdtoNameMapping[message.user_id])) {
                    likee[memberIdtoNameMapping[message.user_id]] += message.favorited_by.Count();
                } else {
                    likee.Add(memberIdtoNameMapping[message.user_id], 0);
                    likee[memberIdtoNameMapping[message.user_id]] += message.favorited_by.Count();
                }

                // Get whose messages are most liked
                foreach (var favoriter in message.favorited_by) {
                    try {
                        if (liker.ContainsKey(memberIdtoNameMapping[favoriter.ToString()])) {
                            liker[memberIdtoNameMapping[favoriter.ToString()]]++;
                        } else {
                            liker.Add(memberIdtoNameMapping[favoriter.ToString()], 0);
                        }
                    } catch {

                    }
                }

                // Get who writes the most messages
                if (totalMessagesPerUser.ContainsKey(memberIdtoNameMapping[message.user_id])) {
                    totalMessagesPerUser[memberIdtoNameMapping[message.user_id]]++;
                } else {
                    totalMessagesPerUser.Add(memberIdtoNameMapping[message.user_id], 0);
                    totalMessagesPerUser[memberIdtoNameMapping[message.user_id]]++;
                }
            }

            var likerToMessagesSentRatio = new Dictionary<string, double>();
            var likeeToMessagesSentRatio = new Dictionary<string, double>();
            foreach (var member in memberIdtoNameMapping.Keys) {

                var likesMadeByGroupMember = 0.0;
                var likesReceivedByGroupMember = 0.0;
                var totalMessagesCreatedByUser = totalMessagesPerUser[memberIdtoNameMapping[member]];
                var memberName = memberIdtoNameMapping[member];

                if (liker.ContainsKey(memberName)) {
                    likesMadeByGroupMember = liker[memberIdtoNameMapping[member]];
                    double ratio1 = likesMadeByGroupMember / totalMessagesCreatedByUser;
                    likerToMessagesSentRatio.Add(memberName, ratio1);
                }

                if (likee.ContainsKey(memberName)) {
                    likesReceivedByGroupMember = likee[memberIdtoNameMapping[member]];
                    double ratio2 = likesReceivedByGroupMember / totalMessagesCreatedByUser;
                    likeeToMessagesSentRatio.Add(memberName, ratio2);
                }
            }

            return (liker, likee, totalMessagesPerUser, likerToMessagesSentRatio, likeeToMessagesSentRatio);
        }

        private static void RegisterMemberNameAndId(string id, string memberName, ref Dictionary<string, string> dict) {
            if (dict.ContainsKey(id)) {
                return;
            } else {
                dict.Add(id, memberName);
            }
            return;
        }
    }
}
