﻿using DotnetSpider.Core;
using DotnetSpider.Core.Monitor;
using DotnetSpider.Core.Pipeline;
using DotnetSpider.Core.Processor;
using DotnetSpider.Core.Scheduler;
using DotnetSpider.Core.Selector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DotnetSpider.Sample
{
	public class Cnblogs
	{
		public static void Run()
		{
			// 定义要采集的 Site 对象, 可以设置 Header、Cookie、代理等
			var site = new Site { EncodingName = "UTF-8" };
			for (int i = 1; i < 5; ++i)
			{
				// 添加初始采集链接
				site.AddStartUrl("http://www.cnblogs.com");
			}

			// 使用内存Scheduler、自定义PageProcessor、自定义Pipeline创建爬虫
			Spider spider = Spider.Create(site,
				new QueueDuplicateRemovedScheduler(),
				new BlogSumaryProcessor(),
				new NewsProcessor()).
				AddPipeline(new MyPipeline()).
				SetThreadNum(1);

			// 启动爬虫
			spider.Run();
			Console.Read();
		}

		private class MyPipeline : BasePipeline
		{
			private static long blogSumaryCount = 0;
			private static long newsCount = 0;

			public override void Process(ResultItems resultItems)
			{
				if (resultItems.GetResultItem("BlogSumary") != null)
				{
					foreach (BlogSumary entry in resultItems.GetResultItem("BlogSumary"))
					{
						blogSumaryCount++;
						Console.WriteLine($"BlogSumary [{blogSumaryCount}] {entry}");
					}
				}

				if (resultItems.GetResultItem("News") != null)
				{
					foreach (News entry in resultItems.GetResultItem("News"))
					{
						newsCount++;
						Console.WriteLine($"News [{newsCount}] {entry}");
					}
				}
				// 可以自由实现插入数据库或保存到文件
			}
		}

		private class BlogSumaryProcessor : BasePageProcessor
		{
			public BlogSumaryProcessor()
			{
				// 定义目标页的筛选
				TargetUrlPatterns = new HashSet<Regex> { new Regex("^http://www\\.cnblogs\\.com/$"), new Regex("http://www\\.cnblogs\\.com/sitehome/p/\\d+") };
			}

			protected override void Handle(Page page)
			{
				// 利用 Selectable 查询并构造自己想要的数据对象
				var blogSummaryElements = page.Selectable.SelectList(Selectors.XPath("//div[@class='post_item']")).Nodes();
				List<BlogSumary> results = new List<BlogSumary>();
				foreach (var blogSummary in blogSummaryElements)
				{
					var video = new BlogSumary();
					video.Name = blogSummary.Select(Selectors.XPath(".//a[@class='titlelnk']")).GetValue();
					video.Url = blogSummary.Select(Selectors.XPath(".//a[@class='titlelnk']/@href")).GetValue();
					video.Author = blogSummary.Select(Selectors.XPath(".//div[@class='post_item_foot']/a[1]")).GetValue();
					video.PublishTime = blogSummary.Select(Selectors.XPath(".//div[@class='post_item_foot']/text()")).GetValue();
					results.Add(video);
				}

				// 以自定义KEY存入page对象中供Pipeline调用
				page.AddResultItem("BlogSumary", results);
			}
		}

		private class NewsProcessor : BasePageProcessor
		{
			public NewsProcessor()
			{
				// 定义目标页的筛选
				TargetUrlPatterns = new HashSet<Regex> { new Regex("^http://www\\.cnblogs\\.com/$"), new Regex("^http://www\\.cnblogs\\.com/news/$"), new Regex("www\\.cnblogs\\.com/news/\\d+") };
			}

			protected override void Handle(Page page)
			{
				// 利用 Selectable 查询并构造自己想要的数据对象
				var newsElements = page.Selectable.SelectList(Selectors.XPath("//div[@class='post_item']")).Nodes();
				List<News> results = new List<News>();
				foreach (var news in newsElements)
				{
					var video = new News();
					video.Name = news.Select(Selectors.XPath(".//a[@class='titlelnk']")).GetValue();
					video.Url = news.Select(Selectors.XPath(".//a[@class='titlelnk']/@href")).GetValue();
					video.PublishTime = news.Select(Selectors.XPath(".//div[@class='post_item_foot']/text()")).GetValue();
					results.Add(video);
				}

				// 以自定义KEY存入page对象中供Pipeline调用
				page.AddResultItem("News", results);
			}
		}

		public class BlogSumary
		{
			public string Name { get; set; }
			public string Author { get; set; }
			public string PublishTime { get; set; }
			public string Url { get; set; }

			public override string ToString()
			{
				return $"{Name}|{Author}|{PublishTime}|{Url}";
			}
		}

		public class News
		{
			public string Name { get; set; }
			public string PublishTime { get; set; }
			public string Url { get; set; }
			public override string ToString()
			{
				return $"{Name}|{PublishTime}|{Url}";
			}
		}
	}
}
