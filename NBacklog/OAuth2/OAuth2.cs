﻿using NBacklog;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NBacklog.OAuth2
{
    public struct OAuth2App
    {
        public string ClientId;
        public string ClientSecret;
        public string RedirectUri;
        public string CredentialsCachePath;
    }

    public struct OAuth2EndPoint
    {
        public string BaseUri;
        public string AuthResource;
        public string QueryTokenResource;
    }

    internal class OAuth2Credentials
    {
        public string AccessToken;
        public string RefreshToken;
        public DateTime Expires;
    }

    internal static class OAuth2Broker
    {
        internal static async Task<OAuth2Credentials> AuthorizeAsync(OAuth2App app, OAuth2EndPoint endPoint)
        {
            var credentials = LoadCredentials(app);
            if (credentials?.AccessToken == null || credentials.Expires < DateTime.Now)
            {
                var server = StartRedirectServer(app.RedirectUri).ConfigureAwait(false);

                var authUri = $"{endPoint.BaseUri.TrimEnd('/')}/{endPoint.AuthResource.TrimStart('/')}?response_type=code&client_id={app.ClientId}";
                Process.Start(authUri);

                var code = await server;

                credentials = await Task.Factory.StartNew(() => GetCredentials(code, app, endPoint)).ConfigureAwait(false);
                SaveCredentials(credentials, app);
            }

            return credentials;
        }

        internal static async Task<OAuth2Credentials> UpdateCredentialsAsync(OAuth2Credentials credentials, OAuth2App app, OAuth2EndPoint endPoint)
        {
            if (credentials?.AccessToken != null && DateTime.Now < credentials.Expires)
            {
                return credentials;
            }

            return await Task.Factory.StartNew(() =>
            {
                var client = new RestClient(endPoint.BaseUri.TrimEnd('/') + '/');
                client.SetJsonNetSerializer();

                var tokenRequest = new RestRequest(endPoint.QueryTokenResource, Method.POST, DataFormat.Json);
                tokenRequest.SetJsonNetSerializer();
                tokenRequest.AddParameter("grant_type", "authorization_code");
                tokenRequest.AddParameter("client_id", app.ClientId);
                tokenRequest.AddParameter("client_secret", app.ClientSecret);
                tokenRequest.AddParameter("refresh_token", credentials.RefreshToken);

                var response = client.Execute<_QueryTokenResult>(tokenRequest);
                var result = response.Data;

                credentials = new OAuth2Credentials()
                {
                    AccessToken = result.access_token,
                    RefreshToken = result.refresh_token,
                    Expires = DateTime.Now + TimeSpan.FromSeconds(result.expires_in),
                };
                SaveCredentials(credentials, app);

                return credentials;
            });
        }

        private static void SaveCredentials(OAuth2Credentials credentials, OAuth2App app)
        {
            using (var writer = new StreamWriter(app.CredentialsCachePath))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(writer, credentials);
            }
        }

        private static OAuth2Credentials LoadCredentials(OAuth2App app)
        {
            if (!File.Exists(app.CredentialsCachePath))
            {
                return null;
            }

            using (var reader = new StreamReader(app.CredentialsCachePath))
            {
                var serializer = new JsonSerializer();
                return serializer.Deserialize(reader, typeof(OAuth2Credentials)) as OAuth2Credentials;
            }
        }

        private static Task<string> StartRedirectServer(string uri)
        {
            var isServerStarted = false;

            var task = Task.Factory.StartNew(() =>
            {
                using (var listener = new HttpListener())
                {
                    listener.Prefixes.Add(uri);
                    listener.Start();

                    isServerStarted = true;

                    var context = listener.GetContext();
                    var code = context.Request.QueryString.Get("code");

                    var res = context.Response;
                    res.StatusCode = 200;
                    res.Close();

                    return code;
                }
            });
            task.ConfigureAwait(false);

            while (!isServerStarted)
            {
                Thread.Sleep(1);
            }

            return task;
        }

        private static OAuth2Credentials GetCredentials(string code, OAuth2App app, OAuth2EndPoint endPoint)
        {
            var client = new RestClient(endPoint.BaseUri.TrimEnd('/') + '/');
            client.SetJsonNetSerializer();

            var tokenRequest = new RestRequest(endPoint.QueryTokenResource, Method.POST, DataFormat.Json);
            tokenRequest.SetJsonNetSerializer();
            tokenRequest.AddParameter("grant_type", "authorization_code");
            tokenRequest.AddParameter("code", code);
            tokenRequest.AddParameter("client_id", app.ClientId);
            tokenRequest.AddParameter("client_secret", app.ClientSecret);

            var response = client.Execute<_QueryTokenResult>(tokenRequest);
            var result = response.Data;

            return new OAuth2Credentials()
            {
                AccessToken = result.access_token,
                RefreshToken = result.refresh_token,
                Expires = DateTime.Now + TimeSpan.FromSeconds(result.expires_in),
            };
        }

        struct _QueryTokenResult
        {
            public string access_token;
            public string token_type;
            public int expires_in;
            public string refresh_token;
        }
    }
}
