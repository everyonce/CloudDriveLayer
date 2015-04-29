using CloudDriveLayer.CloudDriveModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace CloudDriveLayer
{
    public static class CloudDriveOperations
    {
        public class RetryHandler : DelegatingHandler
        {
            private const int MaxRetries = 10;
            public RetryHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            { }
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                HttpResponseMessage response = new HttpResponseMessage();
                for (int i = 0; i < MaxRetries; i++)
                {
                    response = await base.SendAsync(request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                        return response;
                    else
                        Console.WriteLine("needing to retry {0}: {1}", i, response.ReasonPhrase);
                }
                return response;
            }
        }
        public static CloudDriveListResponse<T> listSearch<T>(ConfigOperations.ConfigData config, String command)
        {
            CloudDriveListResponse<T> currentResult, totalResult=null;
            String nextToken=String.Empty;
            do
            {
                HttpClient request = createAuthenticatedClient(config, config.metaData.metadataUrl);
                String mycontent = request.GetStringAsync(command + (String.IsNullOrWhiteSpace(nextToken)?String.Empty:"&startToken="+nextToken)).Result;
                currentResult = JsonConvert.DeserializeObject<CloudDriveListResponse<T>>(mycontent);
                if (totalResult==null)
                    totalResult = currentResult;
                else
                    totalResult.data.AddRange(currentResult.data);
                nextToken = currentResult.nextToken;
                Console.WriteLine("Paging: {0}/{1}", totalResult.data.Count, totalResult.count);
            } while (currentResult.data.Count > 0 && !String.IsNullOrWhiteSpace(nextToken));
            return totalResult;
        }
        public static CloudDriveListResponse<CloudDriveFolder> listFolderSearchByName(ConfigOperations.ConfigData config, String command, String name)
        {
            CloudDriveListResponse<CloudDriveFolder> initialSearch = listSearch<CloudDriveFolder>(config, command);
            initialSearch.data = initialSearch.data.Where(e => e.name == name).ToList();
            initialSearch.count = initialSearch.data.Count;
            return initialSearch;
        }
        public static CloudDriveListResponse<CloudDriveFile>   listFileSearchByName  (ConfigOperations.ConfigData config, String command, String name)
        {
            CloudDriveListResponse<CloudDriveFile> initialSearch = listSearch<CloudDriveFile>(config, command);
            initialSearch.data = initialSearch.data.Where(e => e.name == name).ToList();
            initialSearch.count = initialSearch.data.Count;
            return initialSearch;
        }
        public static CloudDriveListResponse<CloudDriveNode>   listNodeSearchByName  (ConfigOperations.ConfigData config, String command, String name)
        {
            CloudDriveListResponse<CloudDriveNode> initialSearch = listSearch<CloudDriveNode>(config, command);
            initialSearch.data = initialSearch.data.Where(e => e.name == name).ToList();
            initialSearch.count = initialSearch.data.Count;
            return initialSearch;
        }
        public static T nodeSearch<T>(ConfigOperations.ConfigData config, String command)
        {
            using (HttpClient request = createAuthenticatedClient(config, config.metaData.metadataUrl))
            {
                do
                {
                    Task<String> mycontent = request.GetStringAsync(command);
                    if (!mycontent.IsCanceled && !mycontent.IsFaulted)
                        return JsonConvert.DeserializeObject<T>(mycontent.Result);
                    Console.WriteLine("Request was cancelled, waiting to retry: {0}", command);
                    Thread.Sleep(500);
                } while (true);
            }
        }
        public static T nodeChange<T>(ConfigOperations.ConfigData config, String command, HttpContent body)
        {
            HttpClient request = createAuthenticatedClient(config, config.metaData.metadataUrl);
            body.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var mycontent = request.PutAsync(command, body).Result;
            var result = mycontent.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<T>(result);
        }
        public static HttpClient createAuthenticatedClient(ConfigOperations.ConfigData config, String url)
        {
            HttpClient request = new HttpClient(new RetryHandler(new HttpClientHandler()));
            request.BaseAddress = new Uri(url);
            request.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.lastToken.access_token);
            return request;
        }
        public static CloudDriveListResponse<CloudDriveNode> getChildByName(ConfigOperations.ConfigData config, String parentId, String name)
        {
            if (String.IsNullOrWhiteSpace(parentId) || String.IsNullOrWhiteSpace(name)) return new CloudDriveListResponse<CloudDriveNode>();
            return listNodeSearchByName(config, "nodes/" + parentId + "/children?filters=name:\"" + name + "\"", name);
        }
        public static CloudDriveListResponse<CloudDriveNode> getRootNode(ConfigOperations.ConfigData config)
        {
            return listSearch<CloudDriveNode>(config, "nodes?filters=kind:FOLDER AND isRoot:true");
        }
        public static CloudDriveListResponse<CloudDriveFile> getAllFiles(ConfigOperations.ConfigData config)
        {
            return listSearch<CloudDriveFile>(config, "nodes?filters=kind:FILE");
        }
        public static CloudDriveListResponse<CloudDriveNode> getChildren(ConfigOperations.ConfigData config, String parentId)
        {
            if (String.IsNullOrWhiteSpace(parentId)) return new CloudDriveListResponse<CloudDriveNode>();
            return listSearch<CloudDriveNode>(config, "nodes/" + parentId + "/children");
        }
        public static CloudDriveListResponse<CloudDriveFolder> getChildFolderByName(ConfigOperations.ConfigData config, String parentId, String name)
        {
            if (String.IsNullOrWhiteSpace(parentId) || String.IsNullOrWhiteSpace(name)) return new CloudDriveListResponse<CloudDriveFolder>();
            return listFolderSearchByName(config, "nodes/" + parentId + "/children?filters=kind:FOLDER AND name:\"" + name + "\"", name);

        }
        public static CloudDriveListResponse<CloudDriveFolder> getFoldersByName(ConfigOperations.ConfigData config, String name)
        {
            return listFolderSearchByName(config, "nodes?filters=kind:FOLDER AND name:\"" + name + "\"", name);
        }
        public static CloudDriveListResponse<CloudDriveFolder> getRootFolder(ConfigOperations.ConfigData config)
        {
            return listSearch<CloudDriveFolder>(config, "nodes?filters=kind:FOLDER AND isRoot:true");
        }
        public static CloudDriveListResponse<CloudDriveFolder> getFolders(ConfigOperations.ConfigData config, String id)
        {
            return listSearch<CloudDriveFolder>(config, id.Length > 0 ? "nodes/" + id + "/children?filters=kind:FOLDER" : "nodes?filters=kind:FOLDER");
        }
        public static CloudDriveListResponse<CloudDriveFile> getFileByNameAndParentId(ConfigOperations.ConfigData config, String parentId, String name)
        {
            return listFileSearchByName(config, "nodes/" + parentId + "/children?filters=kind:FILE AND name:\"" + name + "\"", name);
        }
        public static CloudDriveListResponse<CloudDriveFile> getFilesByName(ConfigOperations.ConfigData config, String name)
        {
            return listFileSearchByName(config, "nodes?filters=kind:FILE AND name:\"" + name + "\"", name);
        }
        public static CloudDriveListResponse<CloudDriveFile> getFileByNameAndMd5(ConfigOperations.ConfigData config, String name, String md5)
        {
            return listFileSearchByName(config, "nodes?filters=kind:FILE AND name:\"" + name + "\" AND contentProperties.md5:" + md5, name);
        }
        public static CloudDriveFile getFile(ConfigOperations.ConfigData config, String id)
        {
            return nodeSearch<CloudDriveFile>(config, "nodes/" + id);
        }
        public static CloudDriveFile _getFileFromPath(ConfigOperations.ConfigData _config, Queue<String> folders, string findFromId, List<String> traverseQueue, MemoryCache folderCache, CacheItemPolicy cachePolicy)
        {
            try
            {
                var currentFolder = folders.Dequeue();
                traverseQueue.Add(currentFolder);
                var currentPath = String.Join("\\", traverseQueue);
                CacheItem item = folderCache.GetCacheItem(currentPath);
                if (item == null)
                {
                    CloudDriveListResponse<CloudDriveNode> SearchResults;
                    if (currentFolder == "Root") SearchResults = getRootNode(_config);
                    else SearchResults = CloudDriveOperations.getChildByName(_config, findFromId, currentFolder);
                    if (SearchResults == null || SearchResults.count == 0)
                        return null;
                    CloudDriveNode thisNode = SearchResults.data[0];
                    if (thisNode.kind == "FOLDER")
                    {
                        CloudDriveFolder x = CloudDriveModels.Convert.createCloudDriveFolder(thisNode);
                        var findChildren = CloudDriveOperations.getChildren(_config, thisNode.id);
                        if (findChildren.count > 0)
                            x.children = findChildren.data;
                        item = new CacheItem(currentPath, x);
                        folderCache.Add(item, cachePolicy);
                    }
                    else
                    {
                        CloudDriveFile x = CloudDriveModels.Convert.createCloudDriveFile(thisNode);
                        item = new CacheItem(currentPath, x);
                        folderCache.Add(item, cachePolicy);
                    }
                }
                if (folders.Count == 0)
                    return (CloudDriveFile)item.Value;
                else
                    return _getFileFromPath(_config, folders, ((CloudDriveNode)item.Value).id, traverseQueue, folderCache, cachePolicy);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}", e.Message);
                return null;
            }
        }
        public static CloudDriveFile getFileFromPath(ConfigOperations.ConfigData config, String filename, String TopDirectoryId, MemoryCache folderCache, CacheItemPolicy cachePolicy)
        {
            var folderList = new Queue<String>();
            folderList.Enqueue("Root");
            foreach (string folder in filename.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
                folderList.Enqueue(folder);
            return CloudDriveOperations._getFileFromPath(config, folderList, TopDirectoryId, new List<String>(), folderCache, cachePolicy);
        }
        public static CloudDriveFolder getFolder(ConfigOperations.ConfigData config, String id)
        {
            return nodeSearch<CloudDriveFolder>(config, "nodes/" + id);
        }
        public static CloudDriveFolder getFolderFromPath(ConfigOperations.ConfigData _config, Queue<String> folders, string findFromId, List<String> traverseQueue, MemoryCache folderCache, CacheItemPolicy cachePolicy)
        {
            var currentFolder = folders.Dequeue();
            traverseQueue.Add(currentFolder);
            var currentPath = String.Join("\\", traverseQueue);
            CacheItem item = folderCache.GetCacheItem(currentPath);
            if (item == null)
            {
                CloudDriveFolder thisNode = (CloudDriveFolder)CloudDriveOperations.getChildFolderByName(_config, findFromId, currentFolder).data[0];
                thisNode.children = CloudDriveOperations.getChildren(_config, thisNode.id).data;
                item = new CacheItem(currentPath, thisNode);
                folderCache.Add(item, cachePolicy);
            }
            if (folders.Count == 0)
                return item.Value as CloudDriveFolder;
            else
                return getFolderFromPath(_config, folders, ((CloudDriveFolder)item.Value).id, traverseQueue, folderCache, cachePolicy);
        }
        public static Task<Stream> getFileContents(ConfigOperations.ConfigData config, String id, RangeItemHeaderValue range)
        {
            HttpClient request = createAuthenticatedClient(config, config.metaData.contentUrl);
            request.DefaultRequestHeaders.Range = new RangeHeaderValue(range.From, range.To);
            return request.GetStreamAsync(String.Format("nodes/{0}/content", id));
           // return x;
        }
        public static String uploadFile(ConfigOperations.ConfigData config, string fullFilePath, string parentId)
        {   return uploadFile(config, fullFilePath, parentId, false); }
        public static String uploadFile(ConfigOperations.ConfigData config, string fullFilePath, string parentId, Boolean force)
        {

            var parentList = new List<String>();
            parentList.Add(parentId);

            Dictionary<string, Object> addNode = new Dictionary<string, Object>() { { "name", Path.GetFileName(fullFilePath) }, { "kind", "FILE" }, {"parents",parentList} };
            String myMetaData = JsonConvert.SerializeObject(addNode, Newtonsoft.Json.Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, });
            using (FileStream file = File.Open(fullFilePath, FileMode.Open, FileAccess.Read))
            {
                MultipartFormDataContent form = new MultipartFormDataContent();
                form.Add(new StringContent(myMetaData), "metadata");

                var fileStreamContent = new StreamContent(file);
                fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(MimeTypeMap.MimeTypeMap.GetMimeType(Path.GetExtension(fullFilePath)));
                form.Add(fileStreamContent, "content", Path.GetFileName(fullFilePath));

                HttpClient request = createAuthenticatedClient(config, config.metaData.contentUrl);
                request.Timeout = new TimeSpan(0,2,0);
                var postAsync = request.PostAsync("nodes" + (force ? "?suppress=deduplication":""), form);
                while (!postAsync.IsCompleted)
                {
                    if (file.CanRead)
                        Console.WriteLine("{0}: {1:P2} uploaded ({2}/{3})", Path.GetFileName(fullFilePath), (double)file.Position / (double)file.Length, file.Position, file.Length);
                    else
                        Console.WriteLine("Can't read file: {0}", fullFilePath);
                    Thread.Sleep(5000);
                }
                Console.WriteLine("{0}: uploaded", Path.GetFileName(fullFilePath));
                if (postAsync.IsCanceled || postAsync.IsFaulted)
                {
                    String message ="";
                    if (postAsync.Exception != null) message = postAsync.Exception.Message;
                    Console.WriteLine("upload POST was cancelled!  Retry later: {0}", message );
                    return String.Empty;
                }
                HttpResponseMessage result = postAsync.Result;
                if (result.StatusCode == HttpStatusCode.Conflict)
                {
                    String errorMessage = result.Content.ReadAsStringAsync().Result;
                    Console.WriteLine("upload POST was cancelled!  Retry later");
                    return String.Empty;
                }
                if (result.StatusCode == HttpStatusCode.Created)
                    return JsonConvert.DeserializeObject<CloudDriveNode>(result.Content.ReadAsStringAsync().Result).id;
                return String.Empty;
            }
        }
        public static String createFolder(ConfigOperations.ConfigData config, string name, string parentId)
        {
            HttpClient reqAccessToken = new HttpClient();

            Dictionary<String, Object> reqParams = new Dictionary<String, Object>();

            reqParams.Add("name", name);
            reqParams.Add("kind", "FOLDER");
            //reqParams.Add("labels", "");
            //reqParams.Add("properties", "");
            var parentList = new List<String>();
            parentList.Add(parentId);
            reqParams.Add("parents", parentList);
            reqAccessToken.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.lastToken.access_token);
            reqAccessToken.BaseAddress = new Uri(config.metaData.metadataUrl);
            String jsonContent = JsonConvert.SerializeObject(reqParams);
            StringContent requestContent = new StringContent(jsonContent, UTF8Encoding.UTF8, "application/json");
            Task<HttpResponseMessage> responseTask = reqAccessToken.PostAsync("nodes", requestContent);
            HttpResponseMessage response = responseTask.Result;
            String x = response.Content.ReadAsStringAsync().Result;
            dynamic p = JsonConvert.DeserializeObject(x);
            return p.id;
        }
        public static void addNodeParent(ConfigOperations.ConfigData config, string nodeId, string parentId)
        {
            nodeChange<CloudDriveFolder>(config, "nodes/" + parentId + "/children/" + nodeId, new StringContent(""));
        }
        public static void uploadFileContent(ConfigOperations.ConfigData config, string localFilename, string p)
        {
            throw new NotImplementedException();
        }
        public static T _getNodeFromPath<T>(ConfigOperations.ConfigData _config, Queue<String> folders, string findFromId, List<String> traverseQueue, MemoryCache folderCache, CacheItemPolicy cachePolicy)
        {
            try
            {
                var currentFolder = folders.Dequeue();
                traverseQueue.Add(currentFolder);
                var currentPath = String.Join("\\", traverseQueue);
                CacheItem item = folderCache.GetCacheItem(currentPath);
                if (item == null)
                {
                    var SearchResults = CloudDriveOperations.getChildByName(_config, findFromId, currentFolder);
                    if (SearchResults == null || SearchResults.count == 0)
                        return default(T);
                    CloudDriveNode thisNode = SearchResults.data[0];
                    if (thisNode.kind == "FOLDER")
                    {
                        CloudDriveFolder x = CloudDriveModels.Convert.createCloudDriveFolder(thisNode);
                        var findChildren = CloudDriveOperations.getChildren(_config, thisNode.id);
                        if (findChildren.count>0) 
                            x.children = findChildren.data;
                        item = new CacheItem(currentPath, x);
                        folderCache.Add(item, cachePolicy);
                    }
                    else
                    {
                        CloudDriveFile x = CloudDriveModels.Convert.createCloudDriveFile(thisNode);
                        item = new CacheItem(currentPath, x);
                        folderCache.Add(item, cachePolicy);
                    }
                }
                if (folders.Count == 0)
                    return (T)item.Value;
                else
                    return _getNodeFromPath<T>(_config, folders, ((CloudDriveFolder)item.Value).id, traverseQueue, folderCache, cachePolicy);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception {0}", e.Message);
                return default(T);
            }
        }
        public static T getNodeFromPath<T>(ConfigOperations.ConfigData config, String filename, String TopDirectoryId, MemoryCache folderCache, CacheItemPolicy cachePolicy)
        {
            var folderList = new Queue<String>();
            folderList.Enqueue("Root");
            foreach (string folder in filename.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
                folderList.Enqueue(folder);
            return CloudDriveOperations._getNodeFromPath<T>(config, folderList, TopDirectoryId, new List<String>(), folderCache, cachePolicy);
        }
    }
}
