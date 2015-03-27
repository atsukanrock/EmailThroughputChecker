using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Amazon.SimpleEmail;

namespace EmailThroughputChecker.MailSender
{
    internal class AmazonSesApiRawMailSenderNoSdk : MailSenderBase
    {
        private readonly string _accessKey;
        private readonly string _secretKey;

        private readonly SHA256 _sha256;
        private readonly string _endpoint;
        private readonly HttpClient _httpClient;

        public AmazonSesApiRawMailSenderNoSdk()
            : this(ConfigurationManager.AppSettings["AWSAccessKey"],
                   ConfigurationManager.AppSettings["AWSSecretKey"],
                   ConfigurationManager.AppSettings["AWSRegion"]) {}

        public AmazonSesApiRawMailSenderNoSdk(string accessKey, string secretKey, string region)
        {
            _accessKey = accessKey;
            _secretKey = secretKey;

            _sha256 = SHA256.Create();
            _endpoint = string.Format("https://email.{0}.amazonaws.com/", region);
            _httpClient = new HttpClient(new HttpClientHandler {AllowAutoRedirect = false});
        }

        protected override void DoSend(MailMessage message)
        {
            const string newLine = "\n";

            var utcNow = DateTimeOffset.UtcNow;
            var dateString = utcNow.ToString("yyyyMMdd");
            var timestampString = utcNow.ToString("yyyyMMdd'T'HHmmss'Z'");

            const string method = "POST";
            const string service = "ses";
            const string host = "email.us-east-1.amazonaws.com";
            const string region = "us-east-1";
            const string mediaType = "application/x-www-form-urlencoded";
            const string contentType = mediaType + "; charset=utf-8";

            var requestParametersBuilder = new StringBuilder("Action=SendRawEmail");
            for (int i = 0, count = message.To.Count; i < count; i++)
            {
                requestParametersBuilder.AppendFormat(
                    "&Destinations.member.{0}={1}", i + 1, Uri.EscapeDataString(message.To[i].Address));
            }
            requestParametersBuilder.Append("&RawMessage.Data=").Append(ToRawData(message));
            requestParametersBuilder.Append("&Source=").Append(Uri.EscapeDataString(message.From.Address));
            requestParametersBuilder.Append("&Version=2010-12-01");
            var requestParameters = requestParametersBuilder.ToString();
            var payloadHash = ToHexString(_sha256.ComputeHash(Encoding.UTF8.GetBytes(requestParameters)));

            // ************* TASK 1: CREATE A CANONICAL REQUEST *************
            const string canonicalUri = "/";
            const string canonicalQuerystring = "";
            var canonicalHeaders =
                "content-type:" + contentType + newLine +
                "host:" + host + newLine +
                "x-amz-content-sha256:" + payloadHash + newLine +
                "x-amz-date:" + timestampString + newLine;
            const string signedHeaders = "content-type;host;x-amz-content-sha256;x-amz-date";
            var canonicalRequest =
                method + newLine +
                canonicalUri + newLine +
                canonicalQuerystring + newLine +
                canonicalHeaders + newLine +
                signedHeaders + newLine +
                payloadHash;

            // ************* TASK 2: CREATE THE STRING TO SIGN*************
            const string algorithm = "AWS4-HMAC-SHA256";
            var credentialScope = dateString + "/" + region + "/" + service + "/" + "aws4_request";
            var stringToSign =
                algorithm + newLine +
                timestampString + newLine +
                credentialScope + newLine +
                ToHexString(_sha256.ComputeHash(Encoding.UTF8.GetBytes(canonicalRequest)));

            // ************* TASK 3: CALCULATE THE SIGNATURE *************
            var signingKey = GetSigningKey(_secretKey, dateString, region, service);
            var signature = HmacSha256(stringToSign, signingKey);
            var signatureString = ToHexString(signature);

            // ************* TASK 4: ADD SIGNING INFORMATION TO THE REQUEST *************
            var authorizationHeaderValue =
                "Credential=" + _accessKey + '/' + credentialScope + ", " +
                "SignedHeaders=" + signedHeaders + ", " +
                "Signature=" + signatureString;

            Trace.WriteLine("BEGIN REQUEST++++++++++++++++++++++++++++++++++++");
            Trace.WriteLine("Request URL = " + _endpoint);

            using (var request = new HttpRequestMessage(HttpMethod.Post, _endpoint))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(algorithm,
                                                                                                authorizationHeaderValue);
                request.Content = new StringContent(requestParameters, Encoding.UTF8, mediaType);
                request.Content.Headers.Add("X-Amz-Content-SHA256", payloadHash);
                request.Content.Headers.Add("X-Amz-Date", timestampString);

                using (var response = _httpClient.SendAsync(request).ConfigureAwait(false).GetAwaiter().GetResult())
                {
                    var content = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

                    Trace.WriteLine("RESPONSE++++++++++++++++++++++++++++++++++++");
                    Trace.WriteLine(string.Format("Response code: {0} {1}", (int)response.StatusCode,
                                                  response.StatusCode));
                    //Trace.WriteLine(content);

                    if (!response.IsSuccessStatusCode)
                        throw new AmazonSimpleEmailServiceException(content) {StatusCode = response.StatusCode};
                }
            }
        }

        private static string ToRawData(MailMessage message)
        {
            using (var stream = new MemoryStream())
            {
                MailMessageSerializer.Serialize(message, stream);
                return Uri.EscapeDataString(Convert.ToBase64String(stream.ToArray()));
            }
        }

        private static string ToHexString(IEnumerable<byte> bytes)
        {
            return string.Join("", bytes.Select(b => b.ToString("x2")));
        }

        private static byte[] GetSigningKey(String key, String dateStamp, String regionName, String serviceName)
        {
            var kSecret = Encoding.UTF8.GetBytes("AWS4" + key);
            var kDate = HmacSha256(dateStamp, kSecret);
            var kRegion = HmacSha256(regionName, kDate);
            var kService = HmacSha256(serviceName, kRegion);
            var kSigning = HmacSha256("aws4_request", kService);
            return kSigning;
        }

        private static byte[] HmacSha256(String data, byte[] key)
        {
            var hmacSha256 = KeyedHashAlgorithm.Create("HmacSHA256");
            hmacSha256.Key = key;
            return hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        protected override void DisposeManagedResources()
        {
            base.DisposeManagedResources();
            _httpClient.Dispose();
        }
    }
}