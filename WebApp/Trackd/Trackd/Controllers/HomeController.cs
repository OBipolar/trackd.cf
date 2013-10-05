using Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Trackd.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.WhatEverVAriable = "siomething";
            IList<UserMetricEntry> list = new List<UserMetricEntry>();
            list.Add(new UserMetricEntry { Id = 1, EntryTimestamp = DateTime.Now, Metric = new Metric { Id = 1, MetricType = new MetricType { Id = 1, Name = "FreeText" }, Name = "Poop" }, NumericalValue = 0, TextValue = "green", UserId = 1 });
            list.Add(new UserMetricEntry { Id = 1, EntryTimestamp = DateTime.Now, Metric = new Metric { Id = 1, MetricType = new MetricType { Id = 1, Name = "FreeText" }, Name = "Poop" }, NumericalValue = 0, TextValue = "green", UserId = 1 });
            list.Add(new UserMetricEntry { Id = 1, EntryTimestamp = DateTime.Now, Metric = new Metric { Id = 1, MetricType = new MetricType { Id = 1, Name = "FreeText" }, Name = "Poop" }, NumericalValue = 0, TextValue = "green", UserId = 1 });
            list.Add(new UserMetricEntry { Id = 1, EntryTimestamp = DateTime.Now, Metric = new Metric { Id = 1, MetricType = new MetricType { Id = 1, Name = "FreeText" }, Name = "Poop" }, NumericalValue = 0, TextValue = "green", UserId = 1 });
            ViewBag.EntryList = list;
            return View();
        }
    }
}
