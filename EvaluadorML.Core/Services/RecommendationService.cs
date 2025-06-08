using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using EvaluadorML.Core.Models;
using System.Collections.Generic;
using System.Linq;

namespace EvaluadorML.Core.Services
{
    public class RecommendationService
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private List<string> _products;

        public RecommendationService(string dataPath)
        {
            _mlContext = new MLContext();
            var data = _mlContext.Data.LoadFromTextFile<RatingData>(dataPath, hasHeader: true, separatorChar: ',');
            _products = _mlContext.Data.CreateEnumerable<RatingData>(data, reuseRowObject: false)
                .Select(r => r.ProductId).Distinct().ToList();

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("UserId", nameof(RatingData.UserId))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("ProductId", nameof(RatingData.ProductId)))
                .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                    new MatrixFactorizationTrainer.Options
                    {
                        MatrixColumnIndexColumnName = "UserId",
                        MatrixRowIndexColumnName = "ProductId",
                        LabelColumnName = "Label",
                        NumberOfIterations = 20,
                        ApproximationRank = 100
                    }));

            _model = pipeline.Fit(data);
        }

        public List<(string ProductId, float Score)> Recommend(string userId, int topN = 5)
        {
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<RatingData, ProductPrediction>(_model);

            var predictions = _products.Select(pid =>
            {
                var prediction = predictionEngine.Predict(new RatingData { UserId = userId, ProductId = pid });
                return (ProductId: pid, Score: prediction.Score);
            })
            .OrderByDescending(x => x.Score)
            .Take(topN)
            .ToList();

            return predictions;
        }

        private class ProductPrediction
        {
            public float Score { get; set; }
        }
    }
}
