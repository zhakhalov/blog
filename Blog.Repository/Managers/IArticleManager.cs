﻿using Blog.Repository.Models;
using Blog.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blog.Repository.Managers
{
    public interface IArticleManager : IArticleRepository
    {
        ArticleModel GetByUrl(string url);
        List<ArticleModel> GetByUser(string username, int skip, int take);
        List<ArticleModel> GetByTag(string tag, int skip, int take);
        List<ArticleModel> GetNewest(int skip, int take);
        List<ArticleModel> GetTopRated(int skip, int take);
        List<ArticleModel> GetMostViewed(int skip, int take);
        List<ArticleModel> Search(string search);
        long CountByUser(string username);
        long CountByTag(string tag);
        long Count();
        void AddComment(CommentModel comment, string articleId);
        void AddSubComment(CommentModel comment, string commentId);
        void IncreaseViewed(int count, string articleId);
        void RateArticle(RateModel rate, string articleId);
        void RateComment(RateModel rate, string commentId);
        bool ExistsTitleOrUrl(string title, string url);        
    }
}
