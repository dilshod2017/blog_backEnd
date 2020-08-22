using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using posts.Models;

namespace blog.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RouterController : ControllerBase
    {
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>();
        public RouterController(IOptions<PortMapConfiguration> options)
        {

            _options.Add("post", options.Value.post);
            _options.Add("comment", options.Value.comment);
            _options.Add("like", options.Value.Like);
            _options.Add("map", options.Value.map);
            _options.Add("router", options.Value.router);
        }
        [HttpGet]
        public string Get()
        {
            return "router";
        }
        [HttpPost]
        public async Task<ActionResult> Router(blog.Action action)
        {
            using var http = new HttpClient();
            string actionString = "";
            string url = _options[action.to] + action.url;
             
            switch (action.method)
            {
                case "get":
                    if (action.to.ToLower() == "post")
                    {
                        try
                        {
                            var postGetAsync = await http.GetAsync(url);
                            actionString = await postGetAsync.Content.ReadAsStringAsync();
                            if (string.IsNullOrEmpty(actionString))
                            {
                                return BadRequest(new { message = "not found"});
                            }
                            var postList = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(actionString);
                            dynamic posts = null;
                            string likesCountUrl = _options["like"] + "api/likes/post/";
                            string commentCountUrl = _options["comment"] + "api/comments/post/";
                            if(postList.Type != null && postList.Type.ToString() == "Array")
                            {
                                posts = new List<dynamic>();
                                foreach (var item in postList)
                                {
                                    var likesCount = await http.GetAsync(likesCountUrl + item._post_id + "/count");
                                    var likeC = await likesCount.Content.ReadAsStringAsync();
                                    var CommentCount = await http.GetAsync(commentCountUrl + item._post_id + "/count");
                                    var commentC = await CommentCount.Content.ReadAsStringAsync();
                                    try
                                    {
                                        int.TryParse(likeC, out int l);
                                        int.TryParse(commentC, out int c);
                                        dynamic p = new
                                        {
                                            item.post_author_id,
                                            item.post_timeStamp,
                                            item.post_title,
                                            item._post_id,
                                            comments = c,
                                            likes = l
                                        };
                                        posts.Add(p);
                                    }
                                    catch (Exception ex)
                                    {

                                    }
                                }
                            }
                            else
                            {
                                var likesList = await http.GetAsync(likesCountUrl + postList._post_id);
                                var likes = await likesList.Content.ReadAsStringAsync();
                                var CommentList = await http.GetAsync(commentCountUrl + postList._post_id);
                                var comments = await CommentList.Content.ReadAsStringAsync();
                                var mapGetAsync = await http.GetAsync(_options["map"] + "api/mapping/post/" + postList._post_id);
                                string map = await mapGetAsync.Content.ReadAsStringAsync();
                                var deserMap = Newtonsoft.Json.JsonConvert.DeserializeObject<List<dynamic>>(map);
                                posts = new
                                {
                                    postList.post_author_id,
                                    postList.post_timeStamp,
                                    postList.post_title,
                                    postList._post_id,
                                    text = deserMap.FirstOrDefault()?.map,
                                    comments = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(comments),
                                    likes = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(likes)
                                };
                            }
                            actionString = Newtonsoft.Json.JsonConvert.SerializeObject(posts);
                        }
                        catch (Exception ex)
                        {
                            actionString = "Error from try: " + action.ToString() + ", error:: " + ex.Message.ToString();
                        }
                    }else
                    {
                        var item = await http.GetAsync(url);
                        actionString = await item.Content.ReadAsStringAsync();
                    }
                    break;
                case "post":
                     await http.PostAsync(url, new StringContent(action.data, Encoding.UTF8,"application/json"));
                     break;
                case "put":

                    break;
                case "delete":
                    var deleteAsync = await http.DeleteAsync(url);
                    var deletedItem = await deleteAsync.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(deletedItem))
                    {
                        return Ok(actionString);
                    }
                    var listDelCom = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(deletedItem);
                    url = _options["like"] + "api/likes/comment/";
                    foreach (var item in listDelCom)
                    {
                        if(action.to == "post")
                        {
                            var commentList = await http.DeleteAsync(_options["comment"] + "api/comments/post/" + item);
                            var deletedComments = await commentList.Content.ReadAsStringAsync();
                            var list = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(deletedComments);
                            foreach (var comItem in list)
                            {
                                await http.DeleteAsync(_options["like"] + "api/likes/comment/" + comItem);
                            }
                            url = _options["map"] + "api/Mapping/" + item;
                            await http.DeleteAsync(url);
                            url = _options["like"] + "api/likes/post/";
                        }
                        await http.DeleteAsync(url+item);
                    }
                    break;
                default:
                    break;
            }
            return Ok(actionString);
        }
    }
}
