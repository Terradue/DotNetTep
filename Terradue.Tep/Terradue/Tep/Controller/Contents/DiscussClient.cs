using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace Terradue.Tep {
    public class DiscussClient {

        private string ApiKey { get; set; }
        public string ApiUsername { get; set; }
        public string Host { get; set; }

        public DiscussClient(string host) {
            this.Host = host;
        }

        public DiscussClient(string host, string apikey, string apiusername) : this(host) {
            this.ApiKey = apikey;
            this.ApiUsername = apiusername;
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <returns>The category.</returns>
        /// <param name="name">Name.</param>
        public Category GetCategory(string name) {
            CategoryList categories = GetCategories();
            foreach (var category in categories.categories) {
                if (category.name == name) return category;
            }
            return null;
        }

        /// <summary>
        /// Gets the categories.
        /// </summary>
        /// <returns>The categories.</returns>
        public CategoryList GetCategories() {
            CategoryList categories = new CategoryList();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/categories.json", this.Host));
            request.Method = "GET";
            request.ContentType = "application/json";
            request.Proxy = null;

            try {
                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        categories = JsonSerializer.DeserializeFromString<CategoryList>(result);
                    }
                }
            } catch (Exception e) {
                throw e;
            }
            return categories;
        }

        /// <summary>
        /// Posts the topic.
        /// </summary>
        /// <returns>The topic.</returns>
        /// <param name="category">Category.</param>
        /// <param name="subject">Subject.</param>
        /// <param name="body">Body.</param>
        public PostTopicResponse PostTopic(int category, string subject, string body) { 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}/posts", this.Host));
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";
            request.Proxy = null;

            var jsonrequest = new PostTopicRequest {
                title = subject,
                raw = body, category = category
            };

            PostTopicResponse response = new PostTopicResponse();

            string json = JsonSerializer.SerializeToString<PostTopicRequest>(jsonrequest);

            using (var streamWriter = new StreamWriter(request.GetRequestStream())) {
                streamWriter.Write(json);
                streamWriter.Flush();
                streamWriter.Close();

                using (var httpResponse = (HttpWebResponse)request.GetResponse()) {
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream())) {
                        string result = streamReader.ReadToEnd();
                        try {
                            response = JsonSerializer.DeserializeFromString<PostTopicResponse>(result);
                        } catch (Exception e) {
                            throw e;
                        }
                    }
                }
            }
            return response;
        }
    }

    [DataContract]
    public class Category {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string color { get; set; }
        [DataMember]
        public string text_color { get; set; }
        [DataMember]
        public string slug { get; set; }
        [DataMember]
        public int topic_count { get; set; }
        [DataMember]
        public int post_count { get; set; }
        [DataMember]
        public int position { get; set; }
        [DataMember]
        public string description { get; set; }
        [DataMember]
        public string description_text { get; set; }
        [DataMember]
        public string topic_url { get; set; }
        [DataMember]
        public bool read_restricted { get; set; }
        [DataMember]
        public int permission { get; set; }
        [DataMember]
        public object notification_level { get; set; }
        [DataMember]
        public bool can_edit { get; set; }
        [DataMember]
        public string topic_template { get; set; }
        [DataMember]
        public bool has_children { get; set; }
        [DataMember]
        public string sort_order { get; set; }
        [DataMember]
        public bool? sort_ascending { get; set; }
        [DataMember]
        public int topics_day { get; set; }
        [DataMember]
        public int topics_year { get; set; }
        [DataMember]
        public int topics_all_time { get; set; }
        [DataMember]
        public string description_excerpt { get; set; }
        [DataMember]
        public List<int> subcategory_ids { get; set; }
        [DataMember]
        public object uploaded_logo { get; set; }
        [DataMember]
        public object uploaded_background { get; set; }
        [DataMember]
        public bool? is_uncategorized { get; set; }
    }

    [DataContract]
    public class CategoryList {
        [DataMember]
        public bool can_create_category { get; set; }
        [DataMember]
        public bool can_create_topic { get; set; }
        [DataMember]
        public object draft { get; set; }
        [DataMember]
        public string draft_key { get; set; }
        [DataMember]
        public int draft_sequence { get; set; }
        [DataMember]
        public List<Category> categories { get; set; }
    }

    [DataContract]
    public class PostTopicRequest {
        [DataMember]
        public string title { get; set; }
        [DataMember]
        public int topic_idtopic { get; set; }
        [DataMember]
        public string raw { get; set; }
        [DataMember]
        public int category { get; set; }
        [DataMember]
        public string target_usernames { get; set; }
        [DataMember]
        public string archetype { get; set; }
    }

    [DataContract]
    public class PostTopicResponse {
        [DataMember]
        public int id { get; set; }
        [DataMember]
        public string name { get; set; }
        [DataMember]
        public string username { get; set; }
        [DataMember]
        public string avatar_template { get; set; }
        [DataMember]
        public string created_at { get; set; }
        [DataMember]
        public string cooked { get; set; }
        [DataMember]
        public int post_number { get; set; }
        [DataMember]
        public int post_type { get; set; }
        [DataMember]
        public string updated_at { get; set; }
        [DataMember]
        public int reply_count { get; set; }
        [DataMember]
        public object reply_to_post_number { get; set; }
        [DataMember]
        public int quote_count { get; set; }
        [DataMember]
        public object avg_time { get; set; }
        [DataMember]
        public int incoming_link_count { get; set; }
        [DataMember]
        public int reads { get; set; }
        [DataMember]
        public int score { get; set; }
        [DataMember]
        public bool yours { get; set; }
        [DataMember]
        public int topic_id { get; set; }
        [DataMember]
        public string topic_slug { get; set; }
        [DataMember]
        public string display_username { get; set; }
        [DataMember]
        public object primary_group_name { get; set; }
        [DataMember]
        public object primary_group_flair_url { get; set; }
        [DataMember]
        public object primary_group_flair_bg_color { get; set; }
        [DataMember]
        public object primary_group_flair_color { get; set; }
        [DataMember]
        public int version { get; set; }
        [DataMember]
        public bool can_edit { get; set; }
        [DataMember]
        public bool can_delete { get; set; }
        [DataMember]
        public bool can_recover { get; set; }
        [DataMember]
        public bool can_wiki { get; set; }
        [DataMember]
        public object user_title { get; set; }
        [DataMember]
        public List<object> actions_summary { get; set; }
        [DataMember]
        public bool moderator { get; set; }
        [DataMember]
        public bool admin { get; set; }
        [DataMember]
        public bool staff { get; set; }
        [DataMember]
        public int user_id { get; set; }
        [DataMember]
        public int draft_sequence { get; set; }
        [DataMember]
        public bool hidden { get; set; }
        [DataMember]
        public object hidden_reason_id { get; set; }
        [DataMember]
        public int trust_level { get; set; }
        [DataMember]
        public object deleted_at { get; set; }
        [DataMember]
        public bool user_deleted { get; set; }
        [DataMember]
        public object edit_reason { get; set; }
        [DataMember]
        public bool can_view_edit_history { get; set; }
        [DataMember]
        public bool wiki { get; set; }
    }

}
