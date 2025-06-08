using Microsoft.AspNetCore.Mvc;
using EvaluadorML.Core.Services;
using System.IO;

namespace EvaluadorML.Web.Controllers
{
    public class SentimentController : Controller
    {
        private static SentimentService _service;

        public SentimentController()
        {
            if (_service == null)
            {
                var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sentiment-data.tsv");
                _service = new SentimentService(dataPath);
            }
        }

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult Index(string opinion)
        {
            var result = _service.Predict(opinion);
            ViewBag.Result = result;
            return View();
        }
    }
}
