﻿using Blog.Repository.Managers;
using Blog.Repository.Models;
using Blog.Repository.Repositories;
using Blog.WebUI.Code;
using Blog.WebUI.Code.Services;
using Blog.WebUI.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Blog.WebUI.Controllers
{
    public class ArticleController : Controller
    {
        private readonly IArticleManager _articleManager;
        private readonly ITagRepository _tagRepository;
        private readonly ITransliterationService _transliterationService;
        private readonly IArticleConfigService _articleConfigService;

        public ArticleController(
            IArticleManager articleManager, 
            ITagRepository tagRepository,
            ITransliterationService transliterationService,
            IArticleConfigService articleConfigService)
        {
            _articleManager = articleManager;
            _tagRepository = tagRepository;
            _transliterationService = transliterationService;
            _articleConfigService = articleConfigService;
        }

        [AllowAnonymous]
        public ActionResult Read(string url)
        {
            ArticleModel article = null;

            article = _articleManager.GetByUrl(url);
            if (null == article) { throw new HttpException(404, "Not found"); }

            _articleManager.IncreaseViewed(1, article._id.ToString());
            article.Comments = article.Comments.OrderByDescending(c => c.CreateDate).ToList();

            ViewBag.Article = article;
            ViewBag.CommentLimit = _articleConfigService.CommentLimit;
            ViewBag.isCommentEnabled = HttpContext.User.Identity.IsAuthenticated;
            return View("Read");
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            return All(0);
        }

        [AllowAnonymous]
        public ActionResult All(int page = 1)
        {
            long count = _articleManager.Count();
            List<ArticleModel> articles = _articleManager.GetNewest(
                page - 1 * _articleConfigService.PageLimit,
                _articleConfigService.PageLimit);
            articles.ForEach(a => a.Content = _articleConfigService.ShortifyContent(a.Content));
            ViewBag.Articles = articles;
            return View("Articles");
        }

        [AllowAnonymous]
        public ActionResult Tag(string tag, int page = 1)
        {
            //TODO Pagination need
            List<ArticleModel> articles = _articleManager.GetByTag(tag, 0, int.MaxValue);
            articles.ForEach(a => a.Content = _articleConfigService.ShortifyContent(a.Content));
            ViewBag.Title = "Articles by tag " + tag;
            ViewBag.Articles = articles;
            return View("Articles");
        }

        [AllowAnonymous]
        public ActionResult Author(string author)
        {
            //TODO Pagination need
            ViewBag.AllowEdit = User.Identity.IsAuthenticated && (author == User.Identity.Name);
            List<ArticleModel> articles = _articleManager.GetByUser(author, 0, int.MaxValue);
            articles.ForEach(a => a.Content = _articleConfigService.ShortifyContent(a.Content));
            return View("Partial/ArticleList", articles);
        }

        [AllowAnonymous]
        public ActionResult Search(string q)
        {
            List<ArticleModel> articles = _articleManager.Search(q);
            articles.ForEach(a => a.Content = _articleConfigService.ShortifyContent(a.Content));
            ViewBag.Title = "Search: " + q;
            ViewBag.Articles = articles;
            return View("Articles");
        }

        [Authorize]
        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Tags = _tagRepository.GetAll().Select(t => t.Name).ToList();
            ViewBag.TitleLimit = _articleConfigService.TitleLimit;
            return View("Create");
        }

        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(ArticleModel article, string tags)
        {
            article.Author = ((UserModel)Session["user"]).FullName;
            article.Username = ((UserModel)Session["user"]).Username;
            article.Tags = tags.Split(',').ToList();
            article.Raters = new List<RateModel> { new RateModel { Username = article.Username } };
            article.Url = _transliterationService.ToFriendlyUrl(article.Title);
            _articleManager.Save(article);
            return RedirectToAction("Read", new { url = article.Url });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Edit(string info)
        {
            ViewBag.Article = _articleManager.GetByUrl(info);
            ViewBag.Tags = _tagRepository.GetAll().Select(t => t.Name).ToList();
            return View("Edit");
        }

        [Authorize]
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(string content, string articleId, string tags)
        {
            ArticleModel article = _articleManager.GetById(new ObjectId(articleId));
            article.Content = content;
            article.Tags = tags.Split(',').ToList();
            _articleManager.Save(article);
            return RedirectToAction("Read", new { url = article.Url });
        }

        [Authorize]
        [HttpPost]
        public ActionResult CreateTag(string tag)
        {
            dynamic a = new { tag = tag };
            try
            {
                _tagRepository.Save(new TagModel { Name = tag });
            }
            catch
            {
                a.message = string.Format("tag {0} already exists", tag);
                Response.StatusCode = 409;
            }
            return Json(a);
        }

        [Authorize]
        public ActionResult Comment(string id, string comment)
        {
            CommentModel model = new CommentModel
            {
                Username = User.Identity.Name,
                Author = ((UserModel)HttpContext.Session["user"]).FullName,
                Content = comment
            };            
            _articleManager.AddComment(
                comment: model,
                articleId: id);
            return View("Partial/Comment", model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Exists(string title)
        {
            return Json(new { exists = _articleManager.ExistsTitleOrUrl(title, _transliterationService.ToFriendlyUrl(title)) });
        }

        [Authorize]
        public ActionResult Rate(string articleId, bool like)
        {
            _articleManager.RateArticle(
                rate: new RateModel
                {
                    Like = like,
                    Username = HttpContext.User.Identity.Name
                },
                articleId: articleId);
            return Json(new { like = like });
        }
    }
}
