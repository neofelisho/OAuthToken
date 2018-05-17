using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serializers;

namespace OAuthTokenTaker
{
    internal class Program
    {
        private const string OAuthSite = "1st oauth site";
        private const string ClientId = "1st client id";
        private const string ClientSecret = "1st client secret";

        //private const string OAuthSite = "2nd oauth site";
        //private const string ClientId = "2nd client id";
        //private const string ClientSecret = "2nd client secret";

        private const string ReportFileName = "report_name.csv";

        private static void Main()
        {
            var sb = new StringBuilder();
            sb.AppendLine("MemberName,MemberNameTime,ResponseStatus,OAuthTime");

            for (var i = 0; i < 1000; i++)
            {
                var sw = new Stopwatch();
                sw.Start();
                //var memberName = $"loadtest{Counter.Get():00000}";
                string memberName;
                using (var rng = new RNGCryptoServiceProvider())
                {
                    var buffer = new byte[4];
                    rng.GetBytes(buffer);
                    var rn = BitConverter.ToInt32(buffer, 0);
                    rn = rn < 0 ? -rn : rn;
                    memberName = $"loadtest{rn % 100000:00000}";
                }
                sw.Stop();
                Console.WriteLine($"Generate member name: {memberName}, elapsed {sw.ElapsedMilliseconds}ms.");
                sb.Append($"{memberName},{sw.ElapsedMilliseconds},");

                sw.Restart();
                var client = new RestClient(OAuthSite);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddHeader("Cache-Control", "no-cache");
                request.AddParameter("undefined",
                    $"grant_type=password&client_id={ClientId}&client_secret={ClientSecret}&username={memberName}&password={memberName}",
                    ParameterType.RequestBody);
                var response = client.Execute(request);
                sw.Stop();
                Console.WriteLine($"Get oauth token elapsed: {sw.ElapsedMilliseconds}ms.");
                sb.AppendLine($"{response.StatusCode},{sw.ElapsedMilliseconds}");

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    sw.Restart();
                    var deserializer = new JsonDeserializer();
                    var contents = deserializer.Deserialize<List<ResponseBody>>(response);
                    sw.Stop();
                    Console.WriteLine($"Parse json elapsed: {sw.ElapsedMilliseconds}ms.");

                    Console.WriteLine(contents[0].AccessToken);
                }
                else
                {
                    Console.WriteLine($"Failed to get oauth token.");
                }
            }
            using (var sw = new StreamWriter(ReportFileName))
            {
                sw.WriteLine(sb.ToString());
            }

            //Console.Read();
        }
    }

    internal class Counter
    {
        private static int _counter;

        internal static int Get()
        {
            return Interlocked.Increment(ref _counter);
        }
    }

    internal class ResponseBody
    {
        [SerializeAs(Name = "access_token")]
        public string AccessToken { get; set; }

        [SerializeAs(Name = "token_type")]
        public string TokenType { get; set; }

        [SerializeAs(Name = "expires_in")]
        public string ExpiresIn { get; set; }

        [SerializeAs(Name = "refresh_token")]
        public string RefreshToken { get; set; }

        [SerializeAs(Name = "public_key")]
        public string PublicKey { get; set; }
    }
}