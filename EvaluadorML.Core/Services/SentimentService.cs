using Microsoft.ML;
using Microsoft.ML.Data;
using EvaluadorML.Core.Models;

namespace EvaluadorML.Core.Services
{
    public class SentimentService
    {
        private readonly MLContext _mlContext;
        private PredictionEngine<SentimentData, SentimentPrediction> _predEngine;

        public SentimentService(string dataPath)
        {
            _mlContext = new MLContext();
            var data = _mlContext.Data.LoadFromTextFile<SentimentData>(dataPath, hasHeader: true, separatorChar: '\t');
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(SentimentData.Text))
                .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression());
            var model = pipeline.Fit(data);
            _predEngine = _mlContext.Model.CreatePredictionEngine<SentimentData, SentimentPrediction>(model);
        }

        public SentimentPrediction Predict(string text)
        {
            return _predEngine.Predict(new SentimentData { Text = text });
        }
    }
}
