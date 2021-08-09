using System;
using System.Collections.Generic;

namespace Nop.Web.Models.Blogs
{
    public partial class HomePageBlogModel
    {

        public HomePageBlogModel()
        {
            BlogPosts = new List<BlogPostModel>();
        }
        public IList<BlogPostModel> BlogPosts { get; set; }
        public int WorkingLagnuageId { get; set; }
    }
}