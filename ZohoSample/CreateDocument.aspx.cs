using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ZohoSample
{
    public partial class CreateDocument : System.Web.UI.Page
    {


        public static string PostResult = "";
        public static string GetResult = "";

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void brnCreate_Click(object sender, EventArgs e)
        {
            getAccess_Token();
        }

        public void getAccess_Token()
        {


            string refresh_token = "1000.332482fed1114c6b7a47c8071a7e00be.3a3de5562d1353acf31176292d8c956c";
            string client_id = "1000.Z1RPYS1U8VI8MH77HT9ZC5PJGNLHQS";
            string client_secret = "4e6399b7477af30c2801dad6061a71a5db91b41e38";
            string redirect_uri = "https%3A%2F%2Fsign.zoho.com";
            string grant_type = "refresh_token";

            string PathWithoutBaseAddress = "oauth/v2/token?refresh_token=" + refresh_token + "&client_id=" + client_id + "&client_secret=" + client_secret + "&redirect_uri=" + redirect_uri + "&grant_type=" + grant_type;
            // string ActJson = @"[{""refresh_token"":""" + refresh_token + @""",""client_id"":""" + client_id + @""",""client_secret"":""" + client_secret + @""",""redirect_uri"":""" + redirect_uri + @""",""grant_type"":""" + grant_type + @"""}]";
            string ActJson = "";
            string StatusCode = PostFormData(PathWithoutBaseAddress, ActJson);
            //var response = JsonConvert.DeserializeObject<string>(StatusCode);
            var jsonLinq = JObject.Parse(StatusCode);


            var access_token = (string)jsonLinq.SelectToken("access_token");
            var api_domain = (string)jsonLinq.SelectToken("api_domain");
            var token_type = (string)jsonLinq.SelectToken("token_type");
            var expires_in = (string)jsonLinq.SelectToken("expires_in");


            createDocument(access_token);

        }

        public static string PostFormData(string PathWithoutBaseAddress, string ContentJson)
        {
            RunPostAsync(PathWithoutBaseAddress, ContentJson).Wait();
            return GetResult;
        }

        static async Task RunPostAsync(string url, string ContentJson)
        {
            try
            {
                string APIBaseAddress = "https://accounts.zoho.com/";

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(APIBaseAddress);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpContent content = new StringContent(ContentJson);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    DataTable dt = new DataTable();
                    string finalAddress = APIBaseAddress + url;
                    HttpResponseMessage response = await client.PostAsync(finalAddress, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            //PostResult = response.StatusCode.ToString();
                            GetResult = await response.Content.ReadAsStringAsync();
                        }
                        catch (Exception Ex)
                        {
                            string message = Ex.Message;
                        }
                        finally
                        {
                            GC.Collect();
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                string message = Ex.Message;
            }
        }

        public void createDocument(string accessToken)
        {
            string[] files = new string[] { "D:\\test.pdf" }; //PDF to be uploaded

            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");



            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://sign.zoho.com/api/v1/requests");
            request.Headers["Authorization"] = "Zoho-oauthtoken " + accessToken;
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;

            Stream memStream = new System.IO.MemoryStream();

            var boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            var endBoundaryBytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--");

            string formdataTemplate = "\r\n--" + boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}";

            string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n" + "Content-Type: application/pdf\r\n\r\n";

            for (int i = 0; i < files.Length; i++)
            {
                if (File.Exists(files[i]))
                {



                    memStream.Write(boundarybytes, 0, boundarybytes.Length);
                    var header = string.Format(headerTemplate, "file", files[i]);
                    var headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

                    memStream.Write(headerbytes, 0, headerbytes.Length);

                    using (var fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read))
                    {
                        var buffer = new byte[1024];
                        var bytesRead = 0;
                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            memStream.Write(buffer, 0, bytesRead);
                        }
                    }
                }
            }

            memStream.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);
            request.ContentLength = memStream.Length;
            using (Stream requestStream = request.GetRequestStream())
            {
                memStream.Position = 0;
                byte[] tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                memStream.Close();
                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                requestStream.Close();
            }
            JObject requestObj = new JObject();
            using (HttpWebResponse requestResp = (HttpWebResponse)request.GetResponse())
            {
                Stream stream = requestResp.GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string requestResponse = reader.ReadToEnd();
                JObject ResponseString = JObject.Parse(requestResponse);
                requestObj = (JObject)ResponseString.GetValue("requests");
            }
            string requestId = (string)requestObj["request_id"];
            JArray actions = (JArray)requestObj["actions"];

            //Add Recipients
            JObject recipient = new JObject();
            recipient.Add("recipient_name", "Arun Srivastava");
            recipient.Add("recipient_email", "aksrivastava@proind.in");
            recipient.Add("action_type", "SIGN");
            recipient.Add("verify_recipient", false);
            actions.Add(recipient);

            // Add Fields to Recipient
            JArray recipientActions = new JArray();
            for (int i = 0; i < actions.Count; i++)
            {
                JObject action = (JObject)actions[i];
                if (((string)action.GetValue("action_type")).Equals("SIGN"))
                {
                    JArray image_fields = new JArray();
                    JArray date_fields = new JArray();
                    JObject fields = new JObject();
                    JArray documents = (JArray)requestObj["document_ids"];
                    for (int j = 0; j < documents.Count; j++)
                    {
                        JObject document = (JObject)documents[j];
                        string document_id = (string)document.GetValue("document_id");
                        JObject fieldJson = new JObject();

                        //Add a signature field

                        fieldJson.Add("field_name", "Signature");
                        fieldJson.Add("field_label", "Signature");
                        fieldJson.Add("field_type_name", "Signature");
                        fieldJson.Add("page_no", 0);
                        fieldJson.Add("abs_width", "200");
                        fieldJson.Add("abs_height", "18");
                        fieldJson.Add("x_coord", 0 + i * 100);
                        fieldJson.Add("y_coord", 0 + i * 100);
                        fieldJson.Add("document_id", document_id);
                        image_fields.Add(fieldJson);

                        //Add a date field

                        fieldJson = new JObject();
                        fieldJson.Add("field_name", "Sign Date");
                        fieldJson.Add("field_label", "Sign Date");
                        fieldJson.Add("field_type_name", "Date");
                        fieldJson.Add("page_no", 0);
                        fieldJson.Add("abs_width", "200");
                        fieldJson.Add("abs_height", "18");
                        fieldJson.Add("x_coord", 20 + i * 100);
                        fieldJson.Add("y_coord", 20 + i * 100);
                        fieldJson.Add("document_id", document_id);
                        date_fields.Add(fieldJson);
                        fields.Add("image_fields", image_fields);
                        fields.Add("date_fields", date_fields);
                        action.Add("fields", fields);
                        recipientActions.Add(action);
                    }
                }
            }
            requestObj = new JObject();
            requestObj.Add("actions", recipientActions);

            JObject dataJson = new JObject();
            dataJson.Add("requests", requestObj);

            string dataStr = Newtonsoft.Json.JsonConvert.SerializeObject(dataJson);

            //Send Document for Signature
            var Client = new RestClient("https://sign.zoho.com/api/v1/requests/" + requestId + "/submit");
            var submitRequest = new RestRequest();
            submitRequest.AddHeader("Authorization", "Zoho-oauthtoken " + accessToken);
            submitRequest.AddParameter("data", dataStr);
            var response = Client.PostAsync(submitRequest);
            var content = response.Result;
            string responseString = content.ToString();

        }

        public static string PostFormData1(string PathWithoutBaseAddress, string ContentJson)
        {
            RunPostAsync1(PathWithoutBaseAddress, ContentJson).Wait();
            return GetResult;
        }

        static async Task RunPostAsync1(string url, string ContentJson)
        {
            try
            {
                string APIBaseAddress = "https://accounts.zoho.com/";

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri(APIBaseAddress);
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    HttpContent content = new StringContent(ContentJson);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                    DataTable dt = new DataTable();
                    string finalAddress = APIBaseAddress + url;
                    HttpResponseMessage response = await client.PostAsync(finalAddress, content).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            //PostResult = response.StatusCode.ToString();
                            GetResult = await response.Content.ReadAsStringAsync();
                        }
                        catch (Exception Ex)
                        {
                            string message = Ex.Message;
                        }
                        finally
                        {
                            GC.Collect();
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                string message = Ex.Message;
            }
        }


    }
}