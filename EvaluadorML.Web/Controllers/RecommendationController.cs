using Microsoft.AspNetCore.Mvc;
using EvaluadorML.Core.Services;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace EvaluadorML.Web.Controllers
{
    public class ProductRecommendationDto
    {
        public int ProductId { get; set; }
        public float Score { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
    }

    public class RecommendationController : Controller
    {
        private static readonly string DummyJsonProductUrl = "https://dummyjson.com/products/";

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public async Task<IActionResult> Index(string userId, IFormFile ratingsCsv)
        {
            string dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ratings-data.csv");
            // Si se subió un archivo CSV, reemplaza el archivo de entrenamiento
            if (ratingsCsv != null && ratingsCsv.Length > 0)
            {
                using (var stream = new FileStream(dataPath, FileMode.Create))
                {
                    await ratingsCsv.CopyToAsync(stream);
                }
            }

            // Reentrena el modelo cada vez que se recomienda
            var service = new RecommendationService(dataPath);

            // Obtener los productos con los que el usuario ya interactuó
            var interactedProductIds = new HashSet<int>();
            if (System.IO.File.Exists(dataPath))
            {
                var lines = System.IO.File.ReadAllLines(dataPath);
                foreach (var line in lines.Skip(1)) // saltar encabezado
                {
                    var parts = line.Split(',');
                    if (parts.Length >= 3 && parts[0] == userId && int.TryParse(parts[1], out int pid))
                    {
                        interactedProductIds.Add(pid);
                    }
                }
            }

            var recommended = service.Recommend(userId)
                .Select(x => new { ProductId = int.TryParse(x.ProductId, out var id) ? id : 0, x.Score })
                .Where(x => x.ProductId > 0 && !interactedProductIds.Contains(x.ProductId))
                .ToList();

            var result = new List<ProductRecommendationDto>();
            using var client = new HttpClient();
            foreach (var rec in recommended)
            {
                var url = DummyJsonProductUrl + rec.ProductId;
                try
                {
                    var json = await client.GetStringAsync(url);
                    var product = JsonSerializer.Deserialize<DummyJsonProduct>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (product != null)
                    {
                        result.Add(new ProductRecommendationDto
                        {
                            ProductId = product.id,
                            Score = rec.Score,
                            Title = product.title,
                            Description = product.description,
                            Price = product.price,
                            Image = product.thumbnail
                        });
                    }
                }
                catch { /* Si falla la petición, ignora ese producto */ }
            }
            ViewBag.Products = result;
            return View();
        }

        public class DummyJsonProduct
        {
            public int id { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public decimal price { get; set; }
            public string thumbnail { get; set; }
        }
    }
}
