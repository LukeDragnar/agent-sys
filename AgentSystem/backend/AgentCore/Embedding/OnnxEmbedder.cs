using FastBertTokenizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace AgentCore.Embedding;

/// <summary>
/// Load ONNX embedding model (paraphrase-multilingual-MiniLM-L12-v2 hoặc tương đương),
/// sinh vector 384-d cho text input.
///
/// Cấu hình trong appsettings.json (hoặc env):
///   "Embedding": {
///     "ModelPath": "Models/model.onnx",
///     "TokenizerVocabPath": "Models/vocab.txt",
///     "Dim": 384,
///     "MaxTokens": 256
///   }
///
/// File model + vocab tải bằng `scripts/download-embedding-model.sh`.
/// </summary>
public class OnnxEmbedder : IDisposable
{
    private readonly ILogger<OnnxEmbedder> _logger;
    private readonly InferenceSession? _session;
    private readonly BertTokenizer? _tokenizer;
    private readonly int _dim;
    private readonly int _maxTokens;
    private readonly bool _ready;
    private readonly string _modelPath;

    public int Dim => _dim;
    public bool IsReady => _ready;

    public OnnxEmbedder(IConfiguration config, ILogger<OnnxEmbedder> logger)
    {
        _logger = logger;
        _dim = config.GetValue<int>("Embedding:Dim", 384);
        _maxTokens = config.GetValue<int>("Embedding:MaxTokens", 256);

        var baseDir = AppContext.BaseDirectory;
        _modelPath = Path.Combine(baseDir, config["Embedding:ModelPath"] ?? "Models/model.onnx");
        var vocabPath = Path.Combine(baseDir, config["Embedding:TokenizerVocabPath"] ?? "Models/vocab.txt");

        if (!File.Exists(_modelPath) || !File.Exists(vocabPath))
        {
            _logger.LogWarning(
                "Embedding model chưa có tại {Model} hoặc {Vocab}. " +
                "Chạy scripts/download-embedding-model.sh để tải. " +
                "EmbeddingService sẽ báo lỗi khi bị gọi.",
                _modelPath, vocabPath);
            _ready = false;
            return;
        }

        try
        {
            _session = new InferenceSession(_modelPath);

            _tokenizer = new BertTokenizer();
            using (var vocabStream = File.OpenRead(vocabPath))
            {
                _tokenizer.LoadVocabularyAsync(
                    new StreamReader(vocabStream),
                    convertInputToLowercase: true).GetAwaiter().GetResult();
            }

            _ready = true;
            _logger.LogInformation("OnnxEmbedder loaded: {Model} (dim={Dim})", _modelPath, _dim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load ONNX embedder thất bại");
            _ready = false;
        }
    }

    /// <summary>Batch embed. Trả về list float[] theo thứ tự input.</summary>
    public List<float[]> Embed(IReadOnlyList<string> texts)
    {
        if (!_ready || _session == null || _tokenizer == null)
            throw new InvalidOperationException(
                $"OnnxEmbedder chưa sẵn sàng. Model file: {_modelPath}. " +
                "Chạy scripts/download-embedding-model.sh trước.");

        var results = new List<float[]>(texts.Count);

        foreach (var text in texts)
        {
            var (inputIds, attentionMask, tokenTypeIds) = Tokenize(text);

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids",
                    new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length })),
                NamedOnnxValue.CreateFromTensor("attention_mask",
                    new DenseTensor<long>(attentionMask, new[] { 1, attentionMask.Length })),
                NamedOnnxValue.CreateFromTensor("token_type_ids",
                    new DenseTensor<long>(tokenTypeIds, new[] { 1, tokenTypeIds.Length })),
            };

            using var outputs = _session.Run(inputs);
            // Output: "last_hidden_state" shape [1, seq, dim]
            var tensor = outputs.First().AsTensor<float>();
            var seqLen = tensor.Dimensions[1];
            var dim = tensor.Dimensions[2];

            // Mean pooling với attention mask
            var pooled = new float[dim];
            float maskSum = 0;
            for (int t = 0; t < seqLen; t++)
            {
                if (attentionMask[t] == 0) continue;
                maskSum++;
                for (int d = 0; d < dim; d++)
                    pooled[d] += tensor[0, t, d];
            }
            if (maskSum > 0)
                for (int d = 0; d < dim; d++) pooled[d] /= maskSum;

            // L2 normalize
            float norm = 0;
            for (int d = 0; d < dim; d++) norm += pooled[d] * pooled[d];
            norm = (float)Math.Sqrt(norm);
            if (norm > 0)
                for (int d = 0; d < dim; d++) pooled[d] /= norm;

            results.Add(pooled);
        }

        return results;
    }

    private (long[] ids, long[] mask, long[] types) Tokenize(string text)
    {
        var (inputIds, attentionMask, tokenTypeIds) = _tokenizer!.Encode(text, _maxTokens);
        return (
            inputIds.ToArray(),
            attentionMask.ToArray(),
            tokenTypeIds.ToArray()
        );
    }

    public void Dispose() => _session?.Dispose();
}
