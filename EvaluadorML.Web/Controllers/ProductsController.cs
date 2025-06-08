using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EvaluadorML.Core.Services;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace EvaluadorML.Web.Controllers
{
    public class ProductsController : Controller
    {
        private static readonly string ApiUrl = "https://dummyjson.com/products?limit=100";
        private static SentimentService _sentimentService;
        private static string RatingsPath => Path.Combine(Directory.GetCurrentDirectory(), "Data", "ratings-data.csv");

        public ProductsController()
        {
            if (_sentimentService == null)
            {
                var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "sentiment-data.tsv");
                _sentimentService = new SentimentService(dataPath);
            }
        }

        public async Task<IActionResult> Index()
        {
            var products = await GetProductsAsync();
            return View(products);
        }

        [HttpPost]
        public async Task<IActionResult> Opinion(int productId, int? rating)
        {
            var products = await GetProductsAsync();
            var product = products.FirstOrDefault(p => p.id == productId);
            if (product == null)
            {
                ViewBag.Error = "Producto no encontrado";
                return View("Index", products);
            }

            // Guardar rating en ratings-data.csv si se proporcionó
            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5 && User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.Name;
                var ratingLine = $"{userId},{productId},{rating.Value}";
                Directory.CreateDirectory(Path.GetDirectoryName(RatingsPath));
                System.IO.File.AppendAllLines(RatingsPath, new[] { ratingLine });
            }

            ViewBag.Product = product;
            ViewBag.UserId = User.Identity.IsAuthenticated ? User.Identity.Name : null;
            ViewBag.Rating = rating;
            return View("OpinionResult");
        }

        private async Task<List<ProductDto>> GetProductsAsync()
        {
            using var client = new HttpClient();
            var json = await client.GetStringAsync(ApiUrl);
            var result = JsonSerializer.Deserialize<DummyJsonProductsResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Products ?? new List<ProductDto>();
        }

        public class DummyJsonProductsResponse
        {
            [JsonPropertyName("products")]
            public List<ProductDto> Products { get; set; }
        }

        public class ProductDto
        {
            public int id { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string thumbnail { get; set; } // DummyJSON uses 'thumbnail' for main image
            public decimal price { get; set; }

            // For compatibility with the view
            public string image => thumbnail;
        }
    }
}
