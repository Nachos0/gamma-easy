// Copyright (C) 2026. GPL-3.0-or-later. Normal HTTPS download; browser challenges require the user.
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static class CoopDownload
{
    internal static async Task<string> Fetch()
    {
        string folder=Path.Combine(Program.Home,"coop","downloads"); Directory.CreateDirectory(folder);
        string file=Path.Combine(folder,"xrRazom-1.4-Slim.zip");
        if(File.Exists(file) && CacheAudit.HashFile(file,System.Threading.CancellationToken.None)==CoopManager.PublisherMd5) return file;
        using(var client=new HttpClient())
        {
            client.Timeout=TimeSpan.FromMinutes(15); client.DefaultRequestHeaders.UserAgent.ParseAdd("GammaEasy/0.2.0");
            string html;
            try { html=await client.GetStringAsync("https://www.moddb.com/addons/start/314090"); }
            catch(HttpRequestException) { throw new IOException("ModDB blocked the automatic request. Open the official download page, download Slim or Full Bundle, then use Install from ZIP."); }
            var match=Regex.Match(html,"href=[\"']([^\"']*/downloads/mirror/314090/[^\"']+)[\"']",RegexOptions.IgnoreCase);
            if(!match.Success) throw new IOException("ModDB requires browser verification. Download the official ZIP in your browser, then use Install from ZIP.");
            var uri=new Uri(new Uri("https://www.moddb.com"),WebUtility.HtmlDecode(match.Groups[1].Value));
            if(uri.Scheme!="https" || uri.Host!="www.moddb.com") throw new IOException("Unexpected co-op download link.");
            using(var response=await client.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                using(var input=await response.Content.ReadAsStreamAsync()) using(var output=File.Create(file+".partial")) await input.CopyToAsync(output);
            }
        }
        if(CacheAudit.HashFile(file+".partial",System.Threading.CancellationToken.None)!=CoopManager.PublisherMd5) throw new IOException("Downloaded co-op archive failed its published MD5 check. Use the official browser download.");
        if(File.Exists(file)) File.Delete(file);
        File.Move(file+".partial",file); return file;
    }
}
